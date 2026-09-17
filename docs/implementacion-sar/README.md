# Tablero de implementación SAR

Este tablero ejecuta el [plan de corrección aprobado](../auditoria-2026-09-06/08-plan-de-correccion.md). La unidad de avance es una tarea con código, prueba y evidencia; una casilla marcada no significa aprobación del SAR.

## Estado actual

| Frente | Estado | Puerta | Lista |
|---|---|---|---|
| S00–S01: base y perfil fiscal | En curso | G0 | [00-base-y-perfil.md](00-base-y-perfil.md) |
| S02–S06: calidad y seguridad | En curso | G1 | [01-calidad-y-seguridad.md](01-calidad-y-seguridad.md) |
| S07–S13: núcleo fiscal y contable | En curso | G2 | [02-nucleo-fiscal-contable.md](02-nucleo-fiscal-contable.md) |
| S14–S19: operación hotelera | En curso | G3 | [03-operacion-hotelera.md](03-operacion-hotelera.md) |
| S20–S23: cierre, reportes e impresión | Pendiente | G4 | [04-reportes-impresion.md](04-reportes-impresion.md) |
| S24–S28: interfaz y operación local | En curso | G5 | [05-frontend-operacion-local.md](05-frontend-operacion-local.md) |
| S29–S31: migración y liberación | Pendiente | G6 | [06-migracion-liberacion.md](06-migracion-liberacion.md) |
| D01–D02: contenedores | Diferido por alcance | GD | [06-migracion-liberacion.md](06-migracion-liberacion.md#contenedores-diferidos) |

## Convención de las listas

- `[ ]`: pendiente.
- `[x]`: implementada y con la evidencia enlazada en la misma tarea.
- `BLOQUEADA`: necesita una decisión fiscal, dato real del hotel, dispositivo físico o autorización externa. El bloqueo no impide trabajar en tareas independientes.
- `DIFERIDA`: queda fuera del alcance actual por decisión expresa.

Una tarea solo se marca cuando pasan sus comprobaciones. Si una prueba descubre un defecto, se anota en [incidencias.md](incidencias.md), se enlaza con el hallazgo original y la tarea continúa abierta.

## Orden de trabajo activo

1. Inventariar y respaldar la base real antes de migrarla; resolver DEC-01–DEC-15 con administración y contador.
2. Completar G1: rotación operativa de secretos, baja de usuario según política, prueba del certificado/red Windows y revisión técnica independiente. La [política de sesión y transporte LAN](sesion-y-transporte-lan.md), la [cuenta PostgreSQL restringida](postgresql-minimo-privilegio.md) y la [matriz Caja/Contador](matriz-autorizacion.md) ya tienen aprobación técnica automatizada.
3. Continuar G2 con reglas fiscales versionadas, separación completa de estados, inyección de fallos y aprobación contable.
4. Implementar anticipos, conciliación bancaria, compras e inventario siguiendo S14–S19; conciliar la historia previa antes de migrarla a los auxiliares nuevos.
5. Extender contratos tipados, idempotencia y E2E a todos los flujos críticos; probar impresión física en Windows 11 cuando se disponga del equipo.

## Evidencia viva

- [Registro de ejecución](evidencia/registro.md)
- [Ejecución local de QA](ejecucion-qa-local.md)
- [Configuración local de la API](configuracion-api-local.md)
- [Sesión y transporte seguro en la LAN](sesion-y-transporte-lan.md)
- [PostgreSQL con privilegio mínimo](postgresql-minimo-privilegio.md)
- [Decisiones pendientes del hotel](00-base-y-perfil.md#decisiones-del-hotel)
- [Catálogo de 186 pruebas](../plan-sar-2026-09-07/03-pruebas-y-aprobacion.md)
- [Trazabilidad de 64 hallazgos](../plan-sar-2026-09-07/04-trazabilidad.md)
