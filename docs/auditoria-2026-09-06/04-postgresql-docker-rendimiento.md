# PostgreSQL, Docker, impresión, respaldo y velocidad

### OPS-01 · P0 · El frontend actual no compila

**Ubicación:** [frontend/src/pages/reservations/CheckInPage.tsx:1](/home/vm/Projects/erp-hotel/frontend/src/pages/reservations/CheckInPage.tsx:1); [frontend/src/pages/accounting/ChartOfAccountsPage.tsx:189](/home/vm/Projects/erp-hotel/frontend/src/pages/accounting/ChartOfAccountsPage.tsx:189)

**Evidencia y alcance:** npm run build falla con 7 diagnósticos de sintaxis en CheckInPage(179–191) y ChartOfAccountsPage(384). Hay imports duplicados/JSX incompleto en CheckIn y falta cerrar handleDeleteConfirm antes de filterAccounts en catálogo. No se usó un bundle viejo para ocultar el fallo.

**Impacto:** No puede producirse una versión de frontend desplegable desde el código auditado; entrada de huéspedes y catálogo no se cargan correctamente.

**Corrección propuesta:** Reparar estructura de ambos archivos y después atender los errores adicionales que puedan aparecer en compilación completa. Exigir build limpio como puerta de entrega.

**Criterio de cierre:** npm ci con lock elegido, typecheck y build exitosos; navegar check-in y catálogo del artefacto recién construido y completar flujos de prueba.

### OPS-02 · P0 · El Dockerfile de backend copia el código a una ruta distinta de la compilada

**Ubicación:** [docker/backend/Dockerfile:8](/home/vm/Projects/erp-hotel/docker/backend/Dockerfile:8); [docker/backend/Dockerfile:9](/home/vm/Projects/erp-hotel/docker/backend/Dockerfile:9)

**Evidencia y alcance:** Verificación de rutas: csproj se copia a/src/backend/src/hotel-erp.Api; COPY backend/src/. . coloca Program.cs en/src/hotel-erp.Api. Build vuelve al primer directorio, que no contiene fuentes. No se ejecutó docker build de extremo a extremo; el fallo se deduce del contexto y las instrucciones COPY.

**Impacto:** Construcción nueva del contenedor backend falla por ausencia de código/entrypoint, aunque dotnet build del árbol real funciona.

**Corrección propuesta:** Alinear rutas de COPY y WORKDIR; .dockerignore para excluir bin/obj/node_modules/docs de laboratorio; construir en contexto limpio y publicar solo salida verificada.

**Criterio de cierre:** docker compose build --no-cache exitoso desde checkout limpio y API arrancando con DB nueva.

### OPS-03 · P1 · Nginx no entrega la aplicación y cambia las rutas de la API

**Ubicación:** [docker/nginx/nginx.conf:22](/home/vm/Projects/erp-hotel/docker/nginx/nginx.conf:22); [docker/docker-compose.yml:56](/home/vm/Projects/erp-hotel/docker/docker-compose.yml:56); [docker/frontend/Dockerfile:11](/home/vm/Projects/erp-hotel/docker/frontend/Dockerfile:11)

**Evidencia y alcance:** Proxy exterior sirve/usr/share/nginx/html del nginx estándar sin montar/proxyar frontend. proxy_pass http://api:8080/ dentro de/api/ elimina ese prefijo. Nginx interior del frontend conserva configuración estándar sin fallbackSPA ni proxy API.

**Impacto:** Puerto80 muestra contenido estándar; llamadas/api llegan con ruta incorrecta. Puerto3000 no resuelve la API relativa y recargar rutas profundas falla.

**Corrección propuesta:** Una entrada web coherente: proxy exterior al servicio frontend o servir su build en el mismo nginx; preservar/api y habilitar fallbackSPA en el servidor que realmente contiene los assets.

**Criterio de cierre:** Desde otro equipo LAN: login, llamada/api, navegación y recarga de/invoices funcionan por una única URL.

### OPS-04 · P1 · La impresión usa APIs de Windows dentro del destino Linux

**Ubicación:** [backend/src/hotel-erp.Api/Services/RawPrinterHelper.cs:7](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/RawPrinterHelper.cs:7); [backend/src/hotel-erp.Api/Services/EscPosService.cs:298](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/EscPosService.cs:298); [backend/src/hotel-erp.Api/Controllers/PrintController.cs:29](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/PrintController.cs:29)

**Evidencia y alcance:** T27: GET /print/printers devuelve 500 en Linux. Build emite CA1416 por APIs disponibles solo en Windows. El Dockerfile final usa ASP.NET Linux.

**Impacto:** Facturación no dispone de impresión física soportada en el despliegue previsto. No son advertencias inocuas por usar una sola máquina.

**Corrección propuesta:** Adaptador de impresión compatible con el host real: colaCUPS, ESC/POS TCP o servicio de impresión Windows separado si el hardware lo exige. Resolver imagen/raster sin System.Drawing Windows-only.

**Criterio de cierre:** Prueba en la imagen Linux y modelo real de impresora: enumerar, imprimir, reintentar fallo y evitar doble emisión fiscal.

### OPS-05 · P1 · Recibo impreso no conserva fielmente el documento fiscal emitido

**Ubicación:** [backend/src/hotel-erp.Api/Services/EscPosService.cs:115](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/EscPosService.cs:115); [backend/src/hotel-erp.Api/Controllers/PrintController.cs:149](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/PrintController.cs:149); [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:264](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:264)

**Evidencia y alcance:** Código: título FACTURA genérico, rango inicial fijo, tasa 15/4 y datos CAI actuales en vez de snapshot completo; selección de primera reserva del huésped para fechas. T37: factura de check-in carece de FiscalHash/snapshot por el camino usado. Configuración permite ocultar bloques fiscales/totales.

**Impacto:** Reimpresión puede diferir del documento original o mostrar otra estadía; notas se presentan como facturas y se omiten datos necesarios.

**Corrección propuesta:** Snapshot inmutable completo en todas las rutas, plantilla por tipo de documento y datos de la estadía enlazada. Campos obligatorios no configurables como invisibles; validar formato fiscal con responsable autorizado.

**Criterio de cierre:** Modificar CAI/datos del hotel después de emitir no cambia la reimpresión; nota muestra su tipo y referencia; fechas corresponden a esa reserva.

### OPS-06 · P1 · Respaldo SQL no cubre todos los datos persistentes ni una recuperación del equipo

**Ubicación:** [backend/src/hotel-erp.Api/Services/DatabaseBackupService.cs:107](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/DatabaseBackupService.cs:107); [docker/docker-compose.yml:9](/home/vm/Projects/erp-hotel/docker/docker-compose.yml:9); [backend/src/hotel-erp.Api/Services/BackupOptions.cs:9](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/BackupOptions.cs:9)

**Evidencia y alcance:** Respaldo manual sintético se restauró exitosamente en otra BD local PG18 (10 facturas, 12 líneas, 2 usuarios). No incluye Uploads; Compose no persiste ese directorio ni logs API. RetentionYears no tiene implementación efectiva de retención; rclone requiere configuración externa no provista por el repositorio.

**Impacto:** Recrear contenedor puede perder adjuntos; daño del disco puede perder base y copia local a la vez. Restaurar SQL no recupera impresora, secretos, adjuntos ni configuración.

**Corrección propuesta:** Persistir y respaldar adjuntos/configuración, copia cifrada fuera del equipo, retención verificable, monitoreo de espacio y simulacro en máquina reemplazante. Definir RPO/RTO y UPS para el servidor único.

**Criterio de cierre:** Recuperar en otro equipo desde copia externa: BD+adjuntos+configuración, login, factura, impresión y conciliación; medir tiempo y pérdida máxima de datos.

### OPS-07 · P2 · Procesos de respaldo presentan carreras y fallos de disponibilidad

**Ubicación:** [backend/src/hotel-erp.Api/Services/DatabaseBackupService.cs:105](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/DatabaseBackupService.cs:105); [backend/src/hotel-erp.Api/Services/BackupHostedService.cs:18](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/BackupHostedService.cs:18)

**Evidencia y alcance:** Revisión estática: nombres a resolución de segundos, elección del último respaldo global para descarga, procesos externos sin plazo/terminación robusta y salida redirigida sin drenaje completo en todos los caminos. Reintentos limitados a primeros pendientes pueden postergar otros; errores no contenidos en servicio hospedado pueden detener API. Hora 02:00 depende del host/contenedor.

**Impacto:** Dos respaldos pueden colisionar o devolver archivo equivocado; procesos trabados, disco lleno o nube inaccesible pueden afectar disponibilidad. Estas carreras no fueron reproducidas como carga destructiva.

**Corrección propuesta:** Identificador único por ejecución, exclusión de trabajos, descargar por id, timeout/kill y drenaje asíncrono; capturar errores por ciclo, backoff y cola justa. Zona explícita y compatibilidad pg_dump/servidor.

**Criterio de cierre:** Simular nube caída, proceso colgado, disco lleno y dos solicitudes; API sigue disponible y cada resultado corresponde al trabajo correcto.

### OPS-08 · P2 · Salud y arranque no reflejan preparación real

**Ubicación:** [backend/src/hotel-erp.Api/Program.cs:158](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Program.cs:158); [backend/src/hotel-erp.Api/Program.cs:189](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Program.cs:189)

**Evidencia y alcance:** T43 obtiene 200 del endpoint de salud; el código devuelve un literal sin consulta a BD. Migraciones y seed corren al inicio; catch fatal no garantiza una salida no-cero explícita. Compose espera DBhealthy pero no una API ready.

**Impacto:** Monitoreo puede anunciar servicio sano sin capacidad operativa y un fallo de migración puede producir bucles poco diagnósticos.

**Corrección propuesta:** Separar liveness/readiness; readiness con conexión/migración y dependencias esenciales. Migraciones controladas con respaldo previo y señal clara de fallo; healthchecks de API/proxy.

**Criterio de cierre:** Cortar BD hace readiness 503 sin matar liveness; migración fallida sale con error visible y no acepta solicitudes.

### DB-01 · P1 · Correlativo protegido por lock sigue usando el CAI obsoleto de EF

**Ubicación:** [backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:53](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:53); [backend/src/hotel-erp.Api/Services/PostgresCorrelativeLock.cs:21](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/PostgresCorrelativeLock.cs:21); [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:53](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:53)

**Evidencia y alcance:** T28: dos solicitudes que precargaron el mismo CAI esperan el lock; una responde 201 y la otra 500 por correlativo único. EF conserva valores de la entidad ya seguida dentro del contexto. El índice único sí evitó dos facturas con igual número.

**Impacto:** Errores bajo concurrencia normal y reintentos inciertos. No se atribuye el mismo fallo automáticamente al servicio de DocumentAuthorization que carga dentro del lock.

**Corrección propuesta:** Actualizar/retornar secuencia atómicamente en BD o recargar la entidad dentro del lock y transacción del documento. Claves de idempotencia y respuesta409/reintento controlado.

**Criterio de cierre:** Solicitudes concurrentes producen números únicos consecutivos válidos y ninguna 500 por carrera; rango agotado no permite emisión.

### DB-02 · P1 · Inicio de rango y validación de autorización son inconsistentes

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/CAIController.cs:60](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/CAIController.cs:60); [backend/src/hotel-erp.Api/Controllers/DocumentAuthorizationsController.cs:67](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/DocumentAuthorizationsController.cs:67); [backend/src/hotel-erp.Api/Services/FiscalAuthorizationService.cs:42](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/FiscalAuthorizationService.cs:42)

**Evidencia y alcance:** T08: rango inicial...00000001 emite primero...00000002, porque CurrentCorrelative inicia en InitialRange y luego se incrementa. Validación textual no garantiza igualdad de prefijos/tipo entre límites y no aplica de manera uniforme fecha inicial/final.

**Impacto:** Se omite el primer comprobante autorizado y pueden aceptarse rangos incoherentes o autorizaciones fuera de vigencia.

**Corrección propuesta:** Guardar siguiente número o último emitido con semántica inequívoca; parseo estructurado de rango, prefijo y tipo; validación uniforme de vigencia por fecha fiscal local.

**Criterio de cierre:** Primer número coincide con inicio; último se emite una sola vez; rango invertido, prefijos diferentes o fuera de vigencia son rechazados.

### DB-03 · P1 · Mezcla de UTC, hora hondureña y límites de consulta

**Ubicación:** [backend/src/hotel-erp.Api/Services/HondurasTime.cs:7](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/HondurasTime.cs:7); [backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:30](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:30); [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:69](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:69)

**Evidencia y alcance:** Se resta 6 horas a DateTimeUtc conservando KindUtc; una hora local se serializa con Z como si fuera UTC. Hay columnas timestamp without time zone y comprobaciones de vencimientoUTC frente a fecha local. Reportes usan <=día siguiente incluyendo medianoche siguiente; listado de facturas puede recibir día final a 00:00 y excluir su jornada.

**Impacto:** Documentos caen en fecha/período equivocado y CAI puede vencerse antes de finalizar el día en Honduras. Rangos adyacentes pueden contar la misma medianoche.

**Corrección propuesta:** UTC para instantes reales, DateOnly para fecha fiscal/estadía, zonaAmerica/Tegucigalpa al presentar; intervalos [inicio, finExclusivo) y contrato de fechas documentado.

**Criterio de cierre:** Probar 23:59/00:00 local, último día fiscal y cambio de mes: cada factura pertenece exactamente a un período y CAI vence al momento definido.

### DB-04 · P1 · DTO devuelven líneas vacías y autorizaciones fallan al mapear fechas

**Ubicación:** [backend/src/hotel-erp.Api/Dtos/Mappings/MappingProfile.cs:69](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Dtos/Mappings/MappingProfile.cs:69); [backend/src/hotel-erp.Api/Dtos/Mappings/MappingProfile.cs:64](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Dtos/Mappings/MappingProfile.cs:64); [backend/src/hotel-erp.Api/Dtos/common/InvoiceDtos.cs:18](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Dtos/common/InvoiceDtos.cs:18)

**Evidencia y alcance:** T34: Items API vacío con líneas presentes en BD; InvoiceItems/FolioItems/PurchaseInvoiceItems no se enlazan explícitamente a Items y falta mapa de línea de factura. T17/T35: autorización se inserta pero mapear DueDate DateOnly→DateTime produce 500; listar también falla con una fila.

**Impacto:** Detalles y liquidación muestran cero o sin consumos, no se pueden operar autorizaciones y reintentar creación puede duplicar datos.

**Corrección propuesta:** Mapas explícitos de colecciones y tipos de fecha consistentes; pruebas AssertConfigurationIsValid más round-trip DTO con entidad real y navegación cargada.

**Criterio de cierre:** Crear/listar/detallar cada documento devuelve todas sus líneas y totales; autorizaciónDateOnly crea201 y lista 200 sin escrituras parciales.

### DB-05 · P2 · Listados completos y N+1 de reportes crecerán con el historial

**Ubicación:** [backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:29](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:29); [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:101](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:101); [frontend/src/pages/inventory/InventoryPage.tsx:62](/home/vm/Projects/erp-hotel/frontend/src/pages/inventory/InventoryPage.tsx:62)

**Evidencia y alcance:** Medición local:10 facturas=10.082 bytes y 8–16 ms;5.010=4.998.975 bytes y 397–562 ms, incluso sin líneas nuevas. Reportes hacen una consulta por cuenta y filtran fechas en memoria. Listados incluyen entidades/navegaciones completas y paginan en navegador; inventario carga productos/categorías/movimientos aun en pestaña no visible.

**Impacto:** Memoria, tráfico y espera crecen por años de operación; varias recepciones compiten con backups y reportes en el mismo servidor. Estas cifras no son un SLA del hardware del hotel.

**Corrección propuesta:** Paginación/filtrado/orden estable en SQL, DTO proyectado con AsNoTracking para lectura, agregación por cuenta y período en SQL; carga de detalle bajo demanda y cancelación de solicitudes.

**Criterio de cierre:** Con 50 mil documentos, listado devuelve solo página 50 y tamaño acotado; estado de resultados usa consultas constantes y mediana/p95 acordados medidos en equipo destino.

### DB-06 · P2 · Esquema requiere restricciones e índices de negocio

**Ubicación:** [backend/src/hotel-erp.Api/Database/ApplicationDbContext.cs:66](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/ApplicationDbContext.cs:66); [backend/src/hotel-erp.Api/Migrations/20260820161833_InitialPostgresCreate.cs:964](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Migrations/20260820161833_InitialPostgresCreate.cs:964)

**Evidencia y alcance:** EXPLAIN local consulta24 facturas por día y escanea 5.010, descarta 4.986 (5.34 ms, 1002 buffers). Faltan índices de fecha compuestos alineados con consultas. No hay exclusión temporal de reservas ni protección de stock concurrente; importes numeric sin escala fija/checks suficientes.

**Impacto:** Validación exclusivamente de aplicación permite corrupción por carreras y entradas fuera de dominio; consultas históricas escalan linealmente.

**Corrección propuesta:** Diseñar índices a partir de consultas reales (fecha/id, estado/fecha, cuarto/intervalo), restricciones de integridad y precisión por dominio. Medir planes antes/después; no añadir índices indiscriminadamente.

**Criterio de cierre:** EXPLAIN con volumen real confirma menos lecturas; restricciones rechazan fechas/importes/solapamientos inválidos incluso mediante SQL directo.

### DB-07 · P2 · PostgreSQL ya está implementado, pero falta validar migración de datos históricos

**Ubicación:** [backend/src/hotel-erp.Api/Program.cs:28](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Program.cs:28); [backend/src/hotel-erp.Api/Migrations/20260820161833_InitialPostgresCreate.cs:9](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Migrations/20260820161833_InitialPostgresCreate.cs:9); [backend/src/hotel-erp.Api/Data/hotel.db:1](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Data/hotel.db:1)

**Evidencia y alcance:** El código actual usa Npgsql y migración inicialPostgreSQL; arrancó y migró correctamente en PG18.6 del laboratorio. Compose especificaPG16, no ejecutado aquí. Sigue versionada una SQLite con 4 migraciones y datos de usuario/sesiones; no hay procedimiento de transferencia y conciliación de un hotel existente.

**Impacto:** Confundir base nueva con migración exitosa puede dejar atrás historia o usar datos/bundle anteriores. El arranque en PG18 no certifica la imagenPG16.

**Corrección propuesta:** Definir origen oficial, exportación transformada, saldos iniciales, secuencias fiscales, identidades y conteos/conciliación; ensayo con copia protegida. Probar la versiónPG16 exacta o actualizar el objetivo de manera deliberada.

**Criterio de cierre:** Ensayo de migración con documentos reales anonimizados conserva totales, líneas y correlativos; plan de rollback documentado y aprobado por responsables.


<!-- ANEXO VERIFICADO -->

## Evidencia de rendimiento y recuperación

[Medición completa](evidencia/rendimiento-local.json):3 lecturas antes y 3 después, mismo servidor local.10 facturas→10.082 bytes;5.010→4.998.975 bytes. Los 5.000 documentos extra eran réplicas sintéticas de cabecera sin líneas nuevas y se eliminaron del laboratorio al terminar la medición. No se usaron para conclusiones contables. [Plan SQL](evidencia/explain-facturas.txt): escaneo secuencial, 24 filas útiles y 4.986 descartadas. Unseqscan en tabla pequeña no prueba por sí solo lentitud grave; aquí acompaña al diseño de consulta no acotada y orienta la prueba de índices con volumen real.

[Restauración SQL](evidencia/restauracion-local.json): exit0 en otra base localPG18, 10 facturas/12 líneas/2 usuarios. Esto acredita restaurabilidad del dump sintético; no acredita respaldos automáticos, nube configurada, adjuntos ni recuperación del equipo. No se conectó una cuentaGoogleDrive del hotel.

El formato temporal explica el fallo dehash: [evidencia canónica](evidencia/hash-timestamp.json). PostgreSQL admite precisión de microsegundos, y `timestamp without time zone` no conserva una zona del instante; véase[tipos fecha PostgreSQL 16](https://www.postgresql.org/docs/16/datatype-datetime.html). EF puede devolver una entidad ya seguida sin reemplazar sus valores por los recién consultados; esto es relevante al CAI precargado bajo lock: [tracking de EF](https://learn.microsoft.com/en-us/ef/core/querying/tracking).

La semántica delproxy con URI se contrastó con[documentación nginx](https://nginx.org/en/docs/http/ngx_http_proxy_module.html#proxy_pass). Para reservas concurrentes se propone estudiar[restricciones de exclusión](https://www.postgresql.org/docs/16/ddl-constraints.html) junto con rangos, según estados y política de cancelación reales. No se implementaron cambios de esquema durante la auditoría.
