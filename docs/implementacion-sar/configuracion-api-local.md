# Configuración local de la API

La API rechaza el arranque cuando faltan la conexión PostgreSQL o la clave de firma JWT. Ese rechazo evita distribuir contraseñas utilizables en `appsettings.json`. En Development, los valores se guardan con [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) fuera del repositorio; `hotel-erp.Api.csproj` contiene únicamente el identificador no secreto del almacén.

## Requisitos

- SDK .NET indicado por el proyecto.
- Una instancia PostgreSQL accesible y una base ya creada.
- Para el perfil Development, una cuenta capaz de aplicar las migraciones. La instalación operativa separa esta cuenta de `hotel_erp_app`, según [PostgreSQL con privilegio mínimo](postgresql-minimo-privilegio.md).

Los siguientes comandos se ejecutan desde la raíz del repositorio. Sustituya todos los valores entre `<...>` y no copie credenciales reales a archivos versionados.

## Windows 11 / PowerShell

```powershell
$project = '.\backend\src\hotel-erp.Api\hotel-erp.Api.csproj'
$connection = 'Host=127.0.0.1;Port=5432;Database=hotel_erp;Username=<USUARIO_MIGRADOR_LOCAL>;Password=<CLAVE>;Pooling=true'
$jwtBytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
$jwtSecret = [Convert]::ToBase64String($jwtBytes)

dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $connection --project $project
dotnet user-secrets set 'Jwt:SecretKey' $jwtSecret --project $project
dotnet user-secrets set 'BootstrapAdmin:Username' '<USUARIO_ADMIN_INICIAL>' --project $project
dotnet user-secrets set 'BootstrapAdmin:Email' '<CORREO_ADMIN_INICIAL>' --project $project
dotnet user-secrets set 'BootstrapAdmin:Password' '<CLAVE_INICIAL_DE_12_O_MAS_CARACTERES>' --project $project

$connection = $null
$jwtSecret = $null
```

## Linux / Bash

```bash
api_project='./backend/src/hotel-erp.Api/hotel-erp.Api.csproj'
read -r -s -p 'Contraseña PostgreSQL: ' database_password
echo
read -r -s -p 'Contraseña del administrador inicial: ' bootstrap_password
echo
jwt_secret="$(openssl rand -base64 48 | tr -d '\n')"

dotnet user-secrets set 'ConnectionStrings:DefaultConnection' "Host=127.0.0.1;Port=5432;Database=hotel_erp;Username=<USUARIO_MIGRADOR_LOCAL>;Password=${database_password};Pooling=true" --project "$api_project"
dotnet user-secrets set 'Jwt:SecretKey' "$jwt_secret" --project "$api_project"
dotnet user-secrets set 'BootstrapAdmin:Username' '<USUARIO_ADMIN_INICIAL>' --project "$api_project"
dotnet user-secrets set 'BootstrapAdmin:Email' '<CORREO_ADMIN_INICIAL>' --project "$api_project"
dotnet user-secrets set 'BootstrapAdmin:Password' "$bootstrap_password" --project "$api_project"

unset database_password bootstrap_password jwt_secret
```

Los tres valores `BootstrapAdmin` solo se consumen cuando la base no contiene ningún usuario. La contraseña debe tener al menos 12 caracteres. Después del primer acceso, se debe cambiar desde el flujo previsto por el sistema.

## Arranque y comprobación

```bash
cd backend/src/hotel-erp.Api
dotnet run
```

El perfil `http` define `ASPNETCORE_ENVIRONMENT=Development` y escucha en `http://localhost:5084`. En otra terminal:

```bash
curl --fail http://localhost:5084/health
```

El resultado esperado contiene `"status":"healthy"`. Los mensajes de migraciones y sincronización del catálogo deben terminar antes de usar el frontend.

## Diagnóstico

| Mensaje o síntoma | Causa | Corrección |
|---|---|---|
| `Configure ConnectionStrings__DefaultConnection...` | No existe la cadena en User Secrets ni en el entorno | Configure `ConnectionStrings:DefaultConnection` con los comandos anteriores |
| `Configure Jwt__SecretKey...` | Falta la clave o tiene menos de 32 bytes | Genere una nueva clave aleatoria y guárdela como `Jwt:SecretKey` |
| `Connection refused` o tiempo agotado | PostgreSQL está detenido, el puerto es incorrecto o no es accesible | Inicie PostgreSQL y revise host/puerto antes de reiniciar la API |
| `La base no contiene usuarios...` | Es una base nueva sin las tres propiedades de bootstrap | Configure usuario, correo y contraseña iniciales en User Secrets |
| `address already in use` | Otro proceso ocupa el puerto 5084 | Detenga el proceso anterior o cambie `applicationUrl` solo para el entorno local |

`dotnet user-secrets clear --project <RUTA_PROYECTO>` elimina toda la configuración local del proyecto. Debe usarse al retirar el equipo o antes de reemplazar todas las credenciales. User Secrets es un mecanismo de desarrollo y no sustituye el almacén operativo de secretos para la instalación del hotel.
