# Ejecución local de QA

El proyecto dispone de una cadena única para restaurar dependencias, compilar, ejecutar pruebas, auditar npm, validar lint y generar el frontend de producción.

## Windows 11 / PowerShell

Desde la raíz del repositorio:

```powershell
.\scripts\qa.ps1
```

Para incluir integración, entregue al proceso una conexión de administrador de una instancia PostgreSQL **de pruebas**. La prueba crea una base con nombre aleatorio, aplica migraciones y la elimina al terminar:

```powershell
$env:HOTEL_ERP_TEST_ADMIN_CONNECTION = 'Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<CLAVE_QA>'
.\scripts\qa.ps1
Remove-Item Env:HOTEL_ERP_TEST_ADMIN_CONNECTION
```

No se debe usar una conexión de producción. El usuario indicado necesita permiso temporal para `CREATE DATABASE` y `DROP DATABASE`; la cuenta normal de la aplicación no debe tener esos privilegios.

## Linux / entorno de desarrollo

```bash
HOTEL_ERP_TEST_ADMIN_CONNECTION='Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=<CLAVE_QA>' ./scripts/qa.sh
```

Si la variable no está configurada, el comando informa que omitió integración. Ese resultado sirve para desarrollo rápido, pero no permite aprobar una puerta de liberación.

## Condiciones de aprobación

- Todos los comandos deben terminar con código 0.
- Integración debe mostrar al menos una prueba aprobada y ninguna omitida.
- `npm audit` debe informar cero vulnerabilidades en el umbral configurado.
- El build no debe agregar advertencias nuevas.
- El directorio de trabajo puede contener cambios deliberados, pero `git diff --check` no debe encontrar errores de espacios ni marcadores de conflicto.
