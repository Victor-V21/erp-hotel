# PostgreSQL con privilegio mínimo

La instalación usa tres identidades de base de datos separadas. La API no debe ejecutarse con `postgres`, con el dueño del esquema ni con una cuenta capaz de aplicar migraciones.

| Identidad | Uso | Privilegios |
|---|---|---|
| Administrador PostgreSQL | Crear la base y los dos roles; aplicar concesiones | Uso puntual por infraestructura; no se configura en la API |
| `hotel_erp_migrator` | Ejecutar migraciones EF durante una actualización controlada | Dueño de la base/esquema; sin superusuario, `CREATEDB`, `CREATEROLE`, replicación ni bypass de RLS |
| `hotel_erp_app` | Ejecutar diariamente la API | `CONNECT`, uso del esquema y DML ordinario; sin crear base, esquema, tablas ni temporales |

La cuenta de aplicación recibe `SELECT` e `INSERT` sobre `AuditLogs`, pero no `UPDATE`, `DELETE`, `TRUNCATE`, `REFERENCES` ni `TRIGGER`. Sobre `__EFMigrationsHistory` recibe únicamente `SELECT`. Esto permite escribir nuevos eventos y verificar la versión del esquema sin reescribir la historia ni fabricar una migración aplicada.

## Instalación nueva en Windows 11

Requisitos: PostgreSQL y `psql` en `PATH`, SDK .NET del proyecto y una copia respaldada del repositorio. El manifiesto `dotnet-tools.json` fija `dotnet-ef` 10.0.8.

Desde PowerShell, en la raíz del repositorio:

```powershell
.\scripts\postgresql\Initialize-HotelErpDatabase.ps1 `
  -PostgresHost 127.0.0.1 `
  -PostgresPort 5432 `
  -AdminUser postgres `
  -DatabaseName hotel_erp `
  -MigratorRole hotel_erp_migrator `
  -ApplicationRole hotel_erp_app
```

El script solicita las tres contraseñas sin mostrarlas, exige al menos 20 caracteres para los roles nuevos y realiza, en orden:

1. crea o endurece los roles y crea la base cuyo dueño es el migrador;
2. restaura la herramienta local y ejecuta todas las migraciones con `HOTEL_ERP_MIGRATION_CONNECTION`;
3. retira privilegios públicos, concede el DML mínimo y protege bitácora/historial;
4. restaura las variables de entorno del proceso al terminar.

El script actualiza las contraseñas de ambos roles si ya existen y asigna el dueño de la base al migrador. Por eso está destinado a una instalación nueva o a una intervención previamente respaldada y aprobada. Una base real existente requiere antes inventariar propietarios, extensiones, integraciones y privilegios; no se debe ejecutar el script sobre ella como mecanismo de prueba.

## Configuración diaria de la API

`appsettings.json` establece `Database:ApplyMigrationsOnStartup=false` y el arranque rechaza activar esa opción fuera de Development. La conexión diaria contiene exclusivamente `hotel_erp_app`:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=127.0.0.1;Port=5432;Database=hotel_erp;Username=hotel_erp_app;Password=<SECRETO_APP>;Pooling=true'
$env:Database__ApplyMigrationsOnStartup = 'false'
```

Al iniciar, la API consulta `__EFMigrationsHistory`. Si falta una migración, aborta con una instrucción controlada en vez de intentar DDL con la cuenta diaria. La semilla idempotente de cuentas y roles usa el DML autorizado.

## Actualización de esquema

Durante una ventana de mantenimiento, detener la API, respaldar y suministrar temporalmente la conexión del migrador desde el almacén de secretos operativo:

```powershell
$env:HOTEL_ERP_MIGRATION_CONNECTION = '<CONEXIÓN_DE_hotel_erp_migrator>'
dotnet tool restore
dotnet tool run dotnet-ef database update `
  --project .\backend\src\hotel-erp.Api\hotel-erp.Api.csproj `
  --startup-project .\backend\src\hotel-erp.Api\hotel-erp.Api.csproj
Remove-Item Env:HOTEL_ERP_MIGRATION_CONNECTION
```

Después de cada actualización se vuelve a ejecutar `grant-application-privileges.sql` con el administrador PostgreSQL. Los privilegios predeterminados cubren tablas/secuencias nuevas creadas por el migrador; la reaplicación confirma las excepciones estrictas de auditoría e historial antes de reiniciar la API.

## Verificación SEC11

La integración crea una base y un rol efímeros, migra con el propietario y después arranca la API con la cuenta restringida. Comprueba login y escritura de auditoría, y exige error PostgreSQL `42501` para:

- `CREATE TABLE` en `public`;
- insertar un registro falso en `__EFMigrationsHistory`;
- actualizar o eliminar `AuditLogs`;
- consultar hashes de contraseña en `pg_authid`.

También verifica que la cuenta tenga desactivados superusuario, creación de base/rol, herencia, replicación y bypass RLS; no reciba `CREATE` ni `TEMP` sobre la base; sí pueda conectar, leer el historial e insertar la auditoría producida por el flujo ordinario.

Este control limita el daño de la credencial diaria. La cuenta aún necesita modificar las tablas funcionales del ERP, por lo que no sustituye permisos HTTP, segregación de usuarios, copias de seguridad, monitoreo ni revisión de la cadena hash.
