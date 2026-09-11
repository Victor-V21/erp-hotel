# Lista 00 — base reproducible y perfil fiscal

## S00. Fijar versión y proteger evidencia

- [x] Registrar commit base `8e5bf6c603fb16519ef7652b0dbb610ac7e1c9de`, rama y cambios preexistentes. Evidencia: [registro](evidencia/registro.md#2026-09-07--inicio-de-implementacion).
- [x] Ejecutar build inicial de backend y frontend sin modificar código. Evidencia: backend compila con 10 advertencias; frontend falla con 7 errores sintácticos en 2 archivos.
- [x] Registrar versiones efectivas de .NET SDK, Node, npm y PostgreSQL disponible. Evidencia: [.NET 10.0.302/10.0.10, Node 26.7.0, npm 11.19.0, cliente PostgreSQL 18.6 y PostgreSQL 16 de prueba](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Seleccionar npm como gestor del frontend, validar `package-lock.json` y retirar `pnpm-lock.yaml` después de comparar que no represente trabajo distinto. Evidencia: [build y auditoría npm sobre el lockfile único](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [ ] Inventariar datos reales, históricos, sintéticos y semillas; no abrir ni copiar PII a `docs`.
- [ ] Obtener un respaldo previo a la primera migración y restaurarlo en una base aislada.
- [ ] Registrar hashes/manifiesto de base, adjuntos, versión del esquema y aplicación.
- [ ] Crear procedimiento de recuperación y reconciliación de correlativos.
- [ ] Aprobar Q01 y M01 con evidencia revisada.

## S01. Perfil fiscal

Estado: **BLOQUEADA parcialmente por documentación del hotel**. Las tareas de ingeniería independientes continúan con perfiles sintéticos claramente identificados.

- [ ] DEC-01: RTN, razón social, establecimiento, punto y obligaciones activas.
- [ ] DEC-02: modalidad, registro de autoimpresor/sistema, tipos de documentos, CAI y rangos.
- [ ] DEC-03: catálogo real de productos/servicios y clasificación fiscal.
- [ ] DEC-04: aplicación de tasa turística y condición IHT del hotel.
- [ ] DEC-05: tratamiento de anticipos, depósitos, no-show y momento de emisión.
- [ ] DEC-06: descuentos comerciales, tercera/cuarta edad y reglas de acumulación.
- [ ] DEC-07: retenciones practicadas y sufridas.
- [ ] DEC-08: exoneraciones y soportes.
- [ ] DEC-09: plan de cuentas y método de valoración de inventario.
- [ ] DEC-10: moneda y tipo de cambio; hasta aprobarla, limitar operación real a HNL.
- [ ] DEC-11: anulaciones, números no utilizados y contingencia.
- [ ] DEC-12: tabla de conservación documental.
- [ ] DEC-13: impresora, papel, original/copia y expediente térmico.
- [ ] DEC-14: hardware Windows 11, usuarios simultáneos, RPO y RTO.
- [ ] DEC-15: alcance de ISR, nómina, activos y otras obligaciones.
- [x] Crear un perfil fiscal persistente con estado Borrador/Aprobado/Retirado y vigencia. Evidencia: [migración, API, auditoría y flujo UI](evidencia/registro.md#2026-09-07--lote-3-perfil-fiscal-atomicidad-e-idempotencia).
- [x] Bloquear modo fiscal real mientras la versión del perfil no esté aprobada. Evidencia: [integración PostgreSQL con bloqueo antes y después del retiro](evidencia/registro.md#2026-09-07--lote-3-perfil-fiscal-atomicidad-e-idempotencia).
- [ ] Aprobar casos N01–N06 y puerta G0.

## Criterio de cierre

S00 termina con una base recuperable e identificada. S01 termina con DEC-01–DEC-15 resueltas o justificadas como no aplicables por contador y administración. Ninguna suposición de desarrollo puede sustituir esos documentos.
