[CmdletBinding()]
param(
    [string]$PostgresHost = '127.0.0.1',
    [ValidateRange(1, 65535)]
    [int]$PostgresPort = 5432,
    [string]$AdminUser = 'postgres',
    [string]$DatabaseName = 'hotel_erp',
    [string]$MigratorRole = 'hotel_erp_migrator',
    [string]$ApplicationRole = 'hotel_erp_app',
    [Security.SecureString]$AdminPassword,
    [Security.SecureString]$MigratorPassword,
    [Security.SecureString]$ApplicationPassword
)

$ErrorActionPreference = 'Stop'
$validIdentifier = '^[a-z][a-z0-9_]{2,62}$'
foreach ($entry in @{
    DatabaseName = $DatabaseName
    MigratorRole = $MigratorRole
    ApplicationRole = $ApplicationRole
}.GetEnumerator()) {
    if ($entry.Value -notmatch $validIdentifier) {
        throw "$($entry.Key) debe cumplir $validIdentifier."
    }
}
if ($MigratorRole -eq $ApplicationRole) {
    throw 'MigratorRole y ApplicationRole deben ser distintos.'
}

if (-not $AdminPassword) {
    $AdminPassword = Read-Host 'Contraseña del administrador PostgreSQL' -AsSecureString
}
if (-not $MigratorPassword) {
    $MigratorPassword = Read-Host "Contraseña nueva para $MigratorRole" -AsSecureString
}
if (-not $ApplicationPassword) {
    $ApplicationPassword = Read-Host "Contraseña nueva para $ApplicationRole" -AsSecureString
}

function ConvertFrom-SecureValue {
    param([Security.SecureString]$Value)
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Quote-ConnectionValue {
    param([string]$Value)
    return '"' + $Value.Replace('"', '""') + '"'
}

function Assert-LastExitCode {
    param([string]$Step)
    if ($LASTEXITCODE -ne 0) {
        throw "$Step falló con código $LASTEXITCODE."
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$roleScript = Join-Path $PSScriptRoot 'create-database-and-roles.sql'
$grantScript = Join-Path $PSScriptRoot 'grant-application-privileges.sql'
$project = Join-Path $repositoryRoot 'backend/src/hotel-erp.Api/hotel-erp.Api.csproj'

$adminPlain = ConvertFrom-SecureValue $AdminPassword
$migratorPlain = ConvertFrom-SecureValue $MigratorPassword
$applicationPlain = ConvertFrom-SecureValue $ApplicationPassword
if ($migratorPlain.Length -lt 20 -or $applicationPlain.Length -lt 20) {
    throw 'Las contraseñas de migración y aplicación deben tener al menos 20 caracteres.'
}
$previousPgPassword = $env:PGPASSWORD
$previousMigratorPassword = $env:HOTEL_ERP_MIGRATOR_PASSWORD
$previousApplicationPassword = $env:HOTEL_ERP_APPLICATION_PASSWORD
$previousMigrationConnection = $env:HOTEL_ERP_MIGRATION_CONNECTION

try {
    $env:PGPASSWORD = $adminPlain
    $env:HOTEL_ERP_MIGRATOR_PASSWORD = $migratorPlain
    $env:HOTEL_ERP_APPLICATION_PASSWORD = $applicationPlain

    & psql --no-password --host $PostgresHost --port $PostgresPort --username $AdminUser `
        --dbname postgres --set "database_name=$DatabaseName" --set "migrator_role=$MigratorRole" `
        --set "application_role=$ApplicationRole" --file $roleScript
    Assert-LastExitCode 'Creación de base y roles'

    $env:HOTEL_ERP_MIGRATION_CONNECTION =
        "Host=$(Quote-ConnectionValue $PostgresHost);Port=$PostgresPort;" +
        "Database=$(Quote-ConnectionValue $DatabaseName);Username=$(Quote-ConnectionValue $MigratorRole);" +
        "Password=$(Quote-ConnectionValue $migratorPlain);Pooling=false"

    Push-Location $repositoryRoot
    try {
        & dotnet tool restore
        Assert-LastExitCode 'Restauración de dotnet-ef'
        & dotnet tool run dotnet-ef database update --project $project --startup-project $project
        Assert-LastExitCode 'Migración del esquema'
    }
    finally {
        Pop-Location
    }

    $env:PGPASSWORD = $adminPlain
    & psql --no-password --host $PostgresHost --port $PostgresPort --username $AdminUser `
        --dbname $DatabaseName --set "database_name=$DatabaseName" --set "migrator_role=$MigratorRole" `
        --set "application_role=$ApplicationRole" --file $grantScript
    Assert-LastExitCode 'Concesión de privilegios de aplicación'

    Write-Host 'Base migrada y privilegios aplicados.' -ForegroundColor Green
    Write-Host "Use $ApplicationRole en ConnectionStrings__DefaultConnection y conserve $MigratorRole solo para actualizaciones."
}
finally {
    $env:PGPASSWORD = $previousPgPassword
    $env:HOTEL_ERP_MIGRATOR_PASSWORD = $previousMigratorPassword
    $env:HOTEL_ERP_APPLICATION_PASSWORD = $previousApplicationPassword
    $env:HOTEL_ERP_MIGRATION_CONNECTION = $previousMigrationConnection
    $adminPlain = $null
    $migratorPlain = $null
    $applicationPlain = $null
}
