# Lista 01 — calidad, seguridad y contratos base

## S02. Build y QA

- [x] Corregir sintaxis de `CheckInPage.tsx` y `ChartOfAccountsPage.tsx`. Evidencia: [build de producción](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Ejecutar `npm run build` y conservar resultado. Evidencia: [2.095 módulos transformados sin error](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Ejecutar lint, clasificar todas las incidencias y corregir sin desactivar reglas globales. Evidencia: [`eslint .` finalizó sin incidencias](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Elegir y conservar un solo lockfile npm. Evidencia: [`package-lock.json` validado y `pnpm-lock.yaml` retirado](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Corregir referencia nullable de `InvoiceRepository`. Evidencia: [backend sin advertencias de compilación](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Aislar advertencias de APIs Windows en el adaptador de impresión apropiado. Evidencia: [backend sin advertencias de plataforma](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [ ] Revisar/eliminar referencia redundante `System.Text.Encoding.CodePages` si las pruebas de impresión permiten hacerlo.
- [x] Crear `hotel-erp.UnitTests` y pruebas iniciales del motor monetario. Evidencia: [34 pruebas unitarias aprobadas](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Crear `hotel-erp.IntegrationTests` y fábrica de aplicación sobre PostgreSQL real. Evidencia: [base efímera, migración completa e integración aprobada](evidencia/registro.md#2026-09-07--lote-2-integridad-rotacion-e-integracion-automatizada).
- [ ] Añadir pruebas frontend unitarias y E2E para flujos críticos.
- [ ] Crear comando local único de QA: build, lint, unitarias, integración y E2E seleccionadas. Avance: [`scripts/qa.sh` aprobado y equivalente PowerShell creado](evidencia/registro.md#2026-09-07--lote-2-integridad-rotacion-e-integracion-automatizada); falta integrar la futura suite E2E.
- [ ] Aprobar Q01–Q04.

## S03. Permisos y endpoints

- [x] Crear catálogo estable de permisos y políticas de ASP.NET. Evidencia: [pruebas de políticas y matriz HTTP](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Proteger roles del sistema por identidad estable, sin confiar en nombres editables. Evidencia: [renombrado y eliminación del rol Admin rechazados con 400](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Impedir autoasignación o delegación de permisos superiores. Evidencia: [validación por permisos efectivos del operador](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Mapear todos los métodos de controladores a permiso o justificación pública. Evidencia: [inventario de endpoints y pruebas de cobertura](matriz-autorizacion.md#inventario-de-endpoints).
- [x] Restringir roles, usuarios, configuración fiscal, contabilidad, exportación, auditoría y respaldos. Evidencia: [políticas reflejadas por controlador y recepción rechazada con 403](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [ ] Añadir motivo/aprobación a notas, anulaciones, descuentos extraordinarios, ajustes y reaperturas.
- [x] Aplicar privilegio mínimo a la cuenta PostgreSQL de aplicación. Evidencia: [roles separados, migración fuera del runtime y prueba SEC11](postgresql-minimo-privilegio.md).
- [ ] Aprobar SEC01–SEC05 y SEC11. Avance: SEC02–SEC04 y SEC11 tienen aprobación técnica automatizada; faltan casos y firmas restantes de la puerta.

## S04. Secretos y sesiones

- [x] Retirar secreto JWT, credencial PostgreSQL y contraseña inicial del código/configuración distribuida. Evidencia: [arranque falla si faltan secretos externos](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [ ] Rotar los valores expuestos y crear bootstrap único con cambio de contraseña obligatorio.
- [x] Implementar versión de seguridad/revocación inmediata de usuario. Evidencia: [access token anterior rechazado con 401 después de cambio y cierre](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Rotar refresh tokens y detectar reutilización. Evidencia: [consumo atómico, replay 401 y revocación de la familia](evidencia/registro.md#2026-09-07--lote-2-integridad-rotacion-e-integracion-automatizada).
- [x] Implementar logout de servidor y limpieza completa del cliente. Evidencia: [access y refresh revocados, más llamada del cliente antes de limpiar estado](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Definir cookie/CSRF/CORS/orígenes para la LAN y probarlo. Evidencia: [política, configuración Windows y comprobaciones SEC08](sesion-y-transporte-lan.md).
- [x] Añadir límite de login y errores ProblemDetails sin detalles sensibles. Evidencia: [límites por cliente, recuperación de ventana y excepción interna segura](evidencia/registro.md#2026-09-09--lote-11-límites-de-autenticación-y-adjuntos-fiscales).
- [x] Validar tipo, tamaño y almacenamiento por ID de adjuntos. Evidencia: [SEC09 automatizada con PDF falso, exceso, traversal y permisos](evidencia/registro.md#2026-09-09--lote-11-límites-de-autenticación-y-adjuntos-fiscales).
- [ ] Aprobar SEC06–SEC10. Avance: SEC06–SEC10 tienen comprobación técnica automatizada y SEC07/SEC08 también recorrido de navegador; falta la firma del revisor independiente y la prueba del certificado/red Windows del hotel.

## S05. Dependencias y casos de uso

- [x] Resolver paquetes vulnerables sobre el lockfile final. Evidencia: [`npm audit --audit-level=low`: 0 vulnerabilidades](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [ ] Excluir de futuras entregas DB SQLite, logs, `bin`, `obj` y bundles obsoletos, preservando el trabajo del usuario.
- [ ] Extraer emisión, crédito/débito, cobro, check-in/out, compra, inventario y cierre de controladores.
- [ ] Quitar `SaveChanges` internos que rompan la unidad de trabajo de operaciones compuestas.
- [ ] Inyectar reloj, cálculo, numerador, impresión y respaldo.
- [ ] Aprobar Q04–Q06.

## S06. Fechas y DTO

- [ ] Separar `DateOnly` fiscal de instantes UTC `timestamptz`.
- [ ] Corregir la conversión de Honduras y consultas con intervalo semiabierto.
- [ ] Mapear líneas de factura, folio y compra en todos los DTO de detalle.
- [x] Unificar `DocumentAuthorizationDto.DueDate` como fecha civil. Evidencia: [contrato `DateOnly` probado al crear y consultar autorizaciones](evidencia/registro.md#2026-09-09--lote-9-reembolsos-y-secuencia-de-caja).
- [ ] Tipar enums y corregir `Empresa`/`Gravado`.
- [ ] Crear contrato común de error, paginación y previsualización monetaria.
- [ ] Generar o validar tipos frontend desde el contrato del backend.
- [ ] Aprobar API01–API05 y DAT01–DAT03.

## Puerta G1

- [x] Build, lint y pruebas base pasan desde instalación reproducible. Evidencia: [`npm ci` y cadena completa de QA con código 0](evidencia/registro.md#2026-09-07--lote-2-integridad-rotacion-e-integracion-automatizada).
- [x] Matriz 401/403 aprobada técnicamente para anónimo, recepción, caja, contador y administración. Evidencia: [perfiles, permisos y controles automatizados](matriz-autorizacion.md#controles-automáticos).
- [x] No quedan secretos predeterminados utilizables ni descargas de respaldo para recepción. Evidencia: [configuración externa obligatoria y `/api/backup/logs` devuelve 403 a recepción](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [ ] La baja de un usuario corta sesiones según política.
- [ ] Revisión técnica independiente registrada.
