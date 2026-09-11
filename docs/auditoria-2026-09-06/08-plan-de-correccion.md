# Plan detallado de corrección, adecuación al SAR y aceptación

**Versión del plan:** 1.0, 7 de septiembre de 2026. **Estado:** planificación; las correcciones y las pruebas de aceptación descritas aquí están pendientes. Este documento sustituye el plan breve anterior y conserva los 64 hallazgos de la auditoría como referencia.

**Objetivo:** convertir la versión auditada en un sistema hotelero con operaciones, documentos fiscales, contabilidad y controles coherentes, demostrarlo con pruebas reproducibles y reunir la evidencia para la validación fiscal y operativa del establecimiento. No se debe declarar el sistema aprobado por el SAR por compilar, generar un CAI, imprimir un QR o pasar pruebas internas.

**Base técnica:** commit `8e5bf6c603fb16519ef7652b0dbb610ac7e1c9de`. Antes de implementar, comparar el nuevo árbol con esta base y revalidar los hallazgos afectados por cambios posteriores. PostgreSQL ya está integrado mediante Npgsql; el trabajo pendiente es endurecer el esquema y validar datos, consultas y migraciones.

## 1. Alcance, documentos y responsables

El destino es una máquina Windows 11 para la red del hotel, con Docker previsto posteriormente. **Esta etapa no incluye modificar Dockerfiles, Compose, nginx, imágenes, redes ni volúmenes de contenedores.** Sí incluye corregir el código, probar PostgreSQL sin exigir Docker, preparar la impresión, respaldar los datos y validar funcionalidad en Windows. El despliegue final en Docker tendrá una aceptación adicional pendiente; la aceptación funcional de esta etapa no lo sustituye.

Mantener un backend modular y una base PostgreSQL. Los módulos propuestos son separaciones dentro de la aplicación; no requieren microservicios. La concurrencia de recepción, caja y contabilidad existe aunque haya un solo servidor.

Leer este plan junto con:

- [Matriz normativa y decisiones del hotel](../plan-sar-2026-09-07/01-matriz-normativa-y-decisiones.md): fuentes oficiales, condiciones de aplicabilidad y expediente fiscal.
- [Modelo de datos, transacciones y asientos](../plan-sar-2026-09-07/02-modelo-datos-y-transacciones.md): especificación técnica y ejemplos calculados.
- [Catálogo de pruebas y actas de aprobación](../plan-sar-2026-09-07/03-pruebas-y-aprobacion.md): entradas, resultados exigidos, evidencia y responsables.
- [Trazabilidad de los 64 hallazgos](../plan-sar-2026-09-07/04-trazabilidad.csv): vínculo con pasos, pruebas, puertas y estado inicial.
- [Registro original de hallazgos](hallazgos.json): evidencia de la auditoría, que no debe reescribirse para presentar como cerrados defectos todavía pendientes.

En las rutas siguientes, `B/` significa `backend/src/hotel-erp.Api/` y `F/` significa `frontend/src/`, relativos a la raíz del repositorio. **«Nuevo» identifica un archivo propuesto, todavía inexistente.** Revisar las anclas de métodos del registro de hallazgos: las líneas cambiarán al corregir el código.

| Rol | Responsabilidad y evidencia que debe revisar |
|---|---|
| Responsable técnico / backend | Diseño, migraciones, transacciones, autorización, consultas y revisión de código por otra persona |
| Frontend | Contratos, flujos completos, errores, accesibilidad y presentación fiscal |
| QA | Pruebas independientes, regresiones, concurrencia, fallos y evidencia por versión |
| Contador del hotel / asesor tributario | Perfil fiscal, reglas, plan de cuentas, asientos, libros, formatos y conciliación |
| Administración del hotel | Autorizaciones reales, política comercial, usuarios, caja, custodia y aceptación operativa |
| Recepción / caja / inventario | Ensayo de jornadas, cobros, devoluciones, movimientos y comprobantes físicos |
| Responsable de operación Windows | Impresora, permisos del servicio, respaldo, recuperación y continuidad |
| SAR | Trámites y autorizaciones que correspondan al obligado tributario; no se reemplaza su actuación con una firma interna |

La misma persona puede cubrir varios roles en un hotel pequeño, pero no debe aprobar sola sus propios ajustes monetarios, permisos elevados o reaperturas de períodos. Definir revisión compensatoria de administración cuando no haya suficiente personal.

## 2. Orden de ejecución y puertas de salida

Cada paso se termina con código revisado, migración cuando corresponda, pruebas, documentación y evidencia. No basta con completar la pantalla. Los pasos dependientes pueden diseñarse antes, pero no aprobarse sobre contratos o reglas fiscales sin resolver.

| Etapa | Pasos | Dependencia | Puerta exigida |
|---|---|---|---|
| 0. Identidad y reglas | S00–S01 | Auditoría actual | G0: perfil fiscal y decisiones aplicables documentados |
| 1. Base segura y verificable | S02–S06 | S00; S01 para contratos fiscales | G1: compilación, contratos base y permisos seguros |
| 2. Núcleo fiscal y contable | S07–S13 | G0 + G1 | G2: emisión, notas, redondeo e integridad transaccional demostrados |
| 3. Operación completa | S14–S19 | G2 | G3: reserva, estancia, caja, compras e inventario conciliados |
| 4. Cierres e información | S20–S23 | G2 + G3 | G4: libros, declaraciones aplicables, historia e impresión validados |
| 5. Interfaz y operación local | S24–S28 | Contratos de S07–S23 | G5: experiencia, rendimiento, recuperación y Windows aceptados |
| 6. Datos y liberación | S29–S31 | G0–G5 | G6: migración, jornada y expediente de aceptación completos |
| Posterior: despliegue Docker | D01–D02 | G6 y reanudación explícita de este alcance | GD: aceptación del despliegue final; pendiente en este plan |

**Orden crítico:** S00 → S01 → S06/S07 → S08 → S09/S10 → S11/S12 → S13 → pagos/estancias/compras → libros y declaraciones → aceptación. S02–S05 se pueden desarrollar mientras el contador cierra decisiones, utilizando solo datos sintéticos. No fijar fechas artificiales de finalización: estimar cada paso después de conocer perfil, calidad de datos y equipo disponible.

## 3. Pasos de implementación

### S00. Fijar la versión y proteger la evidencia

**Archivos:** registro de auditoría, archivos de proyecto, lockfiles y documentación de release. **Responsables:** técnico y QA.

1. Registrar SHA, estado del árbol, SDK de .NET, Node, gestor de paquetes y versión principal de PostgreSQL. Conservar los cambios ajenos a esta tarea; no limpiar `obj` ni bases por comodidad.
2. Inventariar si hay documentos reales, datos heredados o únicamente semillas. Separar esas tres categorías y marcar la procedencia de cada conjunto.
3. Antes de cualquier migración, obtener respaldo completo verificable y ejecutar restauración en una base distinta. Guardar solo datos sintéticos o evidencia redactada en Git.
4. Crear la lista de trabajo con los IDs S00–S31 y los 64 hallazgos; asignar responsable y revisor. Añadir nuevos defectos descubiertos sin borrar los anteriores.
5. Crear `docs/implementacion-sar/` para futuras decisiones, actas y resultados por release; registrar allí enlaces a evidencia protegida, no contraseñas ni documentación personal completa.

**Salida:** base identificada, inventario de datos y procedimiento de recuperación disponibles. Pruebas Q01 y M01. S00 no constituye aprobación fiscal.

### S01. Aprobar el perfil fiscal antes de programar reglas definitivas

**Archivos:** especificación normativa anexa; futuros `B/Services/FiscalProfileService.cs` y `B/Database/Entities/FiscalProfileEntities.cs` **(nuevos)**. **Responsables:** contador y administración, con revisión técnica.

1. Completar DEC-01–DEC-15 del anexo: RTN, establecimientos, puntos, modalidad, autorizaciones, obligaciones activas, beneficios fiscales y tratamientos de servicios.
2. Obtener los formatos y reglas vigentes aplicables al hotel. Para autoimpresión, incluir registro del sistema, declaración jurada y documentación requerida; no inventar un proceso de «certificación del ERP» genérico.
3. Construir catálogo de servicios/productos: alojamiento, alimentos, bebidas, lavandería, salones, penalizaciones, propinas y otros que realmente se vendan. Para cada uno definir ISV, tasa turística, exención/exoneración, momento fiscal y cuenta contable.
4. Resolver con ejemplos firmados anticipos, cancelaciones, noches no utilizadas, descuentos legales, precio con/sin impuestos, moneda y redondeo. Distinguir exención del bien, exoneración del comprador y beneficio propio del hotel.
5. Aprobar obligaciones que se implementarán dentro del ERP y aquellas atendidas con auxiliares/exportación y procedimiento externo: ISR, pagos a cuenta, retenciones salariales, activos y otras si aplican. Una obligación omitida no equivale a «no aplica».

**Salida G0:** ficha completa, fuentes/versiones y ejemplos esperados firmados. Las incertidumbres fiscales bloquean la emisión afectada; no se resuelven con valores predeterminados silenciosos. Pruebas N01–N06.

### S02. Recuperar compilación y establecer controles de calidad

**Archivos:** `F/pages/reservations/CheckInPage.tsx`, `F/pages/accounting/ChartOfAccountsPage.tsx`, `frontend/package.json`, lockfiles, proyecto .NET y configuración de calidad.

1. Resolver imports duplicados, JSX incompleto y delimitadores del manejador contable identificados en OPS-01. Compilar desde fuente; los bundles antiguos de `wwwroot` no son evidencia de corrección.
2. Elegir un solo gestor y lockfile autorizado. Hacer instalación reproducible, comprobación de tipos, build y lint. Corregir los 88 errores y evaluar las 5 advertencias reportadas, sin silenciar reglas globalmente.
3. Revisar las 10 advertencias del backend por causa, incluyendo compatibilidad de impresión y versiones. Fijar SDK/dependencias compatibles y documentar las excepciones temporales justificadas.
4. Crear proyectos **nuevos** `backend/tests/hotel-erp.UnitTests` y `backend/tests/hotel-erp.IntegrationTests`; añadir scripts frontend de pruebas unitarias y E2E. Usar una base PostgreSQL de laboratorio, sin exigir contenedores.
5. Incorporar una puerta automatizada reproducible localmente: instalación limpia → análisis → build → unitarias → integración → E2E seleccionadas. Si se usa CI remota, nunca enviar datos del hotel ni secretos.

**Salida:** Q01–Q04 pasan; ninguna entrega puede usar código que no compile o pruebas obligatorias omitidas.

### S03. Corregir privilegios, roles y exposición de información

**Archivos:** `B/Controllers/{Roles,Users,Backup,Accounting,Settings,TaxConfigurations,DataExport,AuditLogs}Controller.cs`, `B/Program.cs`, entidades/repositorios de autenticación; `F/components/layout/ProtectedRoute.tsx`.

1. Sustituir autorización por nombre editable de rol por permisos de identificador estable y políticas de servidor. Proteger roles del sistema y evitar que un usuario se otorgue permisos que no puede delegar.
2. Definir permisos separados para emitir, acreditar/anular, reembolsar, cerrar/reabrir, modificar reglas fiscales, administrar usuarios, leer/exportar PII y descargar/restaurar respaldos.
3. Aplicar las políticas a **cada método de cada controlador**, incluidos catálogos y lecturas sensibles. Crear un inventario endpoint/método/permiso; el menú no es el control de acceso.
4. Exigir autorización elevada y motivo en descuentos extraordinarios, notas, ajustes de stock, reaperturas y diferencias de caja, según montos/política aprobados.
5. Usar una credencial PostgreSQL de aplicación sin superusuario ni DDL; reservar migraciones y respaldos para identidades con privilegios mínimos específicos. Las configuraciones de puertos de Docker quedan en D01.

**Salida:** SEC01–SEC05 y SEC11 pasan para anónimo, recepción, caja, contador y administrador; T42 ya no permite elevación de privilegios.

### S04. Renovar secretos y completar sesiones y validaciones de entrada

**Archivos:** `B/Services/AuthService.cs`, `B/Controllers/AuthController.cs`, configuración y semillas, `F/store/authStore.ts`, `F/lib/axios.ts`, carga de adjuntos y `B/Program.cs`.

1. Retirar claves y contraseñas incrustadas; rotar las que estuvieron expuestas. Implementar bootstrap único, administrado y con cambio de contraseña obligatorio; impedir semillas inseguras en modo real.
2. Incorporar versión de seguridad de usuario/sesión, revocación de acceso tras baja/cambio de contraseña/permisos y rotación de refresh tokens con detección de reutilización.
3. Definir almacenamiento de sesión para la aplicación del hotel: preferir refresh en cookie HttpOnly/Secure con política SameSite y protección CSRF cuando corresponda; acceso breve en memoria. Hacer pruebas explícitas de CORS/orígenes y cierre de sesión en varias pestañas.
4. Llamar al logout del servidor y limpiar cabeceras, caché y sesión local. Un token revocado no puede seguir emitiendo ni descargar archivos.
5. Añadir límites de login, validación de contenido y tamaño de adjuntos, rutas internas por ID, errores ProblemDetails y `traceId` sin stacktraces ni secretos. No registrar tokens, cadenas de conexión o documentos completos.

**Salida:** SEC06–SEC10; la política de transporte seguro en LAN es requisito operativo, aunque su configuración final en Docker esté diferida.

### S05. Reducir dependencias y preparar una arquitectura comprobable

**Archivos:** `B/Services/`, `B/Database/Repositories/`, controladores, manifiestos, `.gitignore`, artefactos generados y README.

1. Corregir o sustituir dependencias vulnerables según uso real, compatibilidad y avisos vigentes. Repetir análisis de ambos ecosistemas sobre el lockfile final; no declarar «seguro» solo por actualizar versiones mayores.
2. Retirar del seguimiento futuro bases SQLite, logs, `bin`, `obj` y bundles que no deban versionarse, conservando respaldos/evidencia y respetando datos ajenos. La retirada del archivo no revoca secretos ya publicados.
3. Crear casos de uso en servicios de aplicación: emisión, nota, cobro, check-in/out, compra, movimiento y cierre. Los controladores autentican, validan contrato y delegan; no calculan impuestos/asientos.
4. Quitar `SaveChanges` autónomos de repositorios usados en una operación compuesta; la unidad de trabajo pertenece al caso de uso. Evitar refactorizar todo el repositorio de una vez: migrar cada flujo con sus regresiones.
5. Inyectar reloj, cálculo, numerador y transportes de impresión/backup. Usar código puro para importes y una base real para restricciones/concurrencia.

**Salida:** Q04–Q06; nueva lógica monetaria no se duplica entre controladores y frontend.

### S06. Unificar DTO, fechas y contratos de API

**Archivos:** `B/Dtos/common/{Invoice,Hotel,Purchase,Settings}Dtos.cs`, demás DTO, mapeos registrados en `B/Program.cs`, `B/Services/HondurasTime.cs`, entidades y `F/types/index.ts`.

1. Separar fecha fiscal `DateOnly` de instante UTC. Persistir instantes como `timestamptz`, fechas como `date`; usar zona `America/Tegucigalpa` mediante un servicio de reloj comprobable y resolución compatible con Windows.
2. Eliminar restas de seis horas que conserven indebidamente `Kind=Utc`. Para consultas diarias, utilizar intervalos semiabiertos: inicio incluido, siguiente inicio excluido.
3. Mapear explícitamente `InvoiceItems`, `FolioItems` y `PurchaseInvoiceItems` a `Items`. Corregir `DocumentAuthorizationDto.DueDate` a fecha civil consistente.
4. Tipar enums y rechazar valores desconocidos; distinguir tipo de comprador y naturaleza tributaria. `Empresa` no debe enviarse a un contrato que admite `Gravado`.
5. Publicar contratos documentados y tipos frontend derivados o comprobados automáticamente. Añadir DTO de previsualización calculada y paginación, y un contrato común de error.
6. Definir si un campo se expresa en lempiras decimales o centavos; no mezclar ambos. El cliente no determina totales, cambio ni impuesto definitivo.

**Salida:** API01–API05 y DAT01–DAT03; los errores de contrato se detectan antes de guardar datos.

### S07. Normalizar autorizaciones y numeración fiscal

**Archivos:** `B/Database/Entities/InvoiceEntities.cs`, `B/Services/{FiscalAuthorizationService,PostgresCorrelativeLock}.cs`, `B/Controllers/{CAI,DocumentAuthorizations}Controller.cs`, `F/pages/invoices/AuthorizationsPage.tsx`.

1. Unificar `CAI` y `DocumentAuthorization` en un agregado de autorización con puente de migración: emisor, establecimiento, punto, tipo, CAI, rango, fechas, estado y evidencia. No eliminar referencias históricas hasta reconciliarlas.
2. Guardar establecimiento/punto/tipo como campos estructurados y correlativo como número; formatear los ceros a la izquierda al presentar. Validar coincidencia de prefijos y límites de un mismo rango; comprobar el formato del CAI y su correspondencia con la autorización oficial, sin considerar suficiente la longitud de 10–50 caracteres del contrato actual.
3. Definir `LastIssued = Initial - 1` para una autorización nueva sin uso; **el primer emitido es Initial**. En datos migrados usar el último número documentalmente conciliado, no el valor defectuoso del contador anterior.
4. Obtener el siguiente número dentro de la transacción mediante actualización atómica con condición de rango/vigencia y `RETURNING`, o bloqueo de fila seguido de lectura fresca. Evitar usar la entidad EF precargada después del lock.
5. Mantener unicidad por emisor y número fiscal completo; documentar excepciones de ciclos únicamente si existe sustento y autorización aplicable. No reiniciar ni reutilizar números desde la UI.
6. Alertar antes de agotamiento/vencimiento; bloquear emisiones fuera de rango, autorización incorrecta o perfil no aprobado. Registrar suspensión/no utilización con motivo y expediente de trámite.

**Salida:** FIS01–FIS07, CON01–CON02; 50 emisiones simultáneas no duplican ni fallan por leer un contador obsoleto.

### S08. Implementar un único motor de precios, descuentos e impuestos

**Archivos:** `B/Services/TaxService.cs`, `B/Database/Entities/InvoiceEntities.cs`, `B/Controllers/{TaxConfigurations,Discounts}Controller.cs`, `F/hooks/useTaxRates.ts` y cálculos de check-in/out/facturas.

1. Sustituir carga síncrona y tasas globales mutables por reglas versionadas con vigencia y clasificación por línea. El servicio recibe la regla aprobada; un fallo de configuración impide emitir.
2. Separar base gravada 15%, base gravada 18%, exenta y exonerada; modelar tasa turística aparte. Aplicarla a conceptos sujetos, no a todo el folio por ser un hotel.
3. Calcular cantidad × precio, descuento permitido, bases e impuestos en servidor. Ignorar/rechazar totales proporcionados por el navegador y valores negativos destinados a simular devoluciones.
4. Definir precisión intermedia y redondeo final, ambos explícitos; distribuir descuentos generales y residuos de manera determinista. No cargar diferencias arbitrarias al ISV para cuadrar un total.
5. Resolver precios inclusivos, exoneración parcial y reglas de acumulación de descuentos según DEC-05/06. Registrar identificación y sustento mínimo del beneficio con acceso restringido.
6. Devolver una previsualización con versión de cotización/regla y vencimiento. Al confirmar, recalcular y rechazar cambios materiales para que el operador revise el nuevo total. El frontend muestra la respuesta del mismo motor.

**Salida:** MON01–MON12; documento, representación, pago y asiento usan los mismos importes. El anexo incluye ejemplos y casos de centavos.

### S09. Modelar el documento fiscal inmutable

**Archivos:** entidades/DTO de facturas, `B/Controllers/InvoicesController.cs`, `B/Database/Repositories/InvoiceRepository.cs`; `B/Services/InvoiceIssuanceService.cs` **(nuevo)**.

1. Separar borrador, emisión y anulación fiscal del estado de cobro, que se deriva de aplicaciones de pagos/créditos. Una nota parcial no convierte la factura original en anulada.
2. Crear instantánea fiscal versionada con emisor, comprador, autorización, líneas, tasas, descuentos, sustento, totales, moneda, fecha y referencias. Guardarla en la emisión y calcular hash canónico sobre esos datos estables.
3. Deshabilitar PUT/PATCH/DELETE sobre contenido de emitidos, incluyendo notas. Corregir mediante operaciones fiscales específicas, nunca actualizando silenciosamente total/hash/asiento.
4. Bloquear creación genérica de NotaCredito/NotaDebito sin origen. Mantener proformas y recibos internos en tipos explícitos sin presentarlos como factura autorizada ni consumir su rango.
5. Crear consulta de historia del documento con notas, pagos, reembolsos, anulaciones e impresiones, sin recalcularlo con catálogos actuales.
6. Añadir restricciones e índices para origen, autorización, número y estado. No usar cascadas destructivas ni filtros globales que oculten documentos emitidos.

**Salida:** FIS08–FIS12 y AUD01; cambiar tasas, nombre comercial o cliente no altera una factura ya emitida.

### S10. Hacer atómica e idempotente cada operación monetaria

**Archivos:** servicios de emisión, reservas, caja, compras e inventario; repositorios; `B/Database/ApplicationDbContext.cs`; entidades **nuevas** de idempotencia y cola persistente.

1. Definir una transacción por caso de uso: validar permisos/estado → bloquear recursos necesarios → calcular → numerar → persistir documento/folio/pago/asiento/auditoría → registrar trabajo de impresión → confirmar.
2. Persistir clave de idempotencia, operación, identidad/autorización, hash de solicitud y resultado. Misma clave/cuerpo devuelve el mismo ID; misma clave con cuerpo distinto devuelve 409. La clave no otorga acceso al resultado a otro usuario.
3. Proteger asignación de folio, crédito disponible, caja y stock con restricciones/bloqueos, en orden consistente para reducir deadlocks. Reintentar únicamente errores transitorios, de forma acotada y sobre toda la transacción.
4. No imprimir, enviar a nube ni llamar servicios externos dentro de la transacción. Persistir trabajo y ejecutarlo después; si la impresora falla, la factura sigue emitida y se reimprime con el mismo número.
5. Si se pierde la respuesta tras el commit, permitir consultar/reintentar por clave. No suponer que un timeout significa que no se cobró.
6. Inyectar fallos en cada punto de persistencia y verificar también tablas secundarias, correlativos y asientos. No reutilizar números de documentos que sí llegaron a emitirse.

**Salida:** TX01–TX08; T03 y T32 ya no dejan una operación parcial. Reintentos concurrentes producen un solo efecto financiero.

### S11. Corregir notas de crédito, débito y anulaciones

**Archivos:** `B/Controllers/InvoicesController.cs`, motor fiscal, servicio de emisión/contabilidad y `F/pages/invoices/{InvoicesPage,InvoiceEditPage}.tsx`.

1. Crear comandos separados para crédito y débito, con autorización del tipo correspondiente, original, líneas/importe afectados, motivo, fecha y datos de recepción exigibles.
2. Crédito parcial: bloquear el saldo acreditable por línea/impuesto; sumar créditos anteriores; impedir superar cantidad, base o impuesto disponible. El crédito total final absorbe solo el residuo fiscal permitido y deja saldo exacto cero.
3. Invertir ingreso/impuestos del crédito. Si la factura está cobrada, crear saldo a favor/devolución pendiente; el reembolso se registra después o conjuntamente como pago real, sin fingir entrega de efectivo.
4. Nota de débito: aumentar deuda e impuestos conforme a su motivo y regla aplicable. Conservar el documento original y todas las referencias fiscales requeridas.
5. Implementar anulación por error como flujo separado, con sustento y conservación de original/copia, según validación del art. 41 y política del contador. No borrar ni reutilizar número; generar reversión contable vinculada cuando corresponda.
6. Registrar la fecha de cada ajuste y tratamiento del período original/corriente. Una nota sobre mes cerrado no habilita editar ese mes.

**Salida:** NC01–NC07 y TX06; crédito total de venta base 100 + ISV 15 deja ingreso e ISV netos en cero, sin anular indebidamente notas parciales.

### S12. Integrar contabilidad con reglas explícitas de partida doble

**Archivos:** `B/Services/AccountingService.cs`, `B/Database/Entities/AccountingEntities.cs`, `B/Database/Repositories/AccountingRepository.cs`, controladores de facturas/compras/caja.

1. Aprobar plan de cuentas y mapeo por evento y categoría, evitando códigos hardcoded dispersos. Resolver la inconsistencia de la cuenta de resultado 3102/3103 mediante migración y política, no renumerando historia arbitrariamente.
2. Emitir asientos desde resultados monetarios definitivos y un origen único. Exigir suma Debe = suma Haber, importes válidos, cuentas imputables, período abierto e identificación de documento/pago.
3. Separar devengo de venta, cobro, anticipo, aplicación del anticipo, devolución, compra, pago, costo de inventario y liquidación de tarjeta. Los ejemplos del anexo son el contrato mínimo.
4. Prohibir borrar un asiento contabilizado. Corregir por reversión y nuevo asiento con referencia y motivo. Los borradores se pueden eliminar sin dejar líneas huérfanas.
5. No contabilizar dos veces por reintento ni por imprimir; índice único de origen/tipo de evento. Los asientos fallidos deben impedir el commit de la operación que los requiere.
6. Permitir conciliación de cada cuenta de control con su auxiliar. Documentar reconocimiento de ingresos y anticipos para estadías entre meses; fecha fiscal y devengo pueden requerir eventos diferentes.

**Salida:** CTA01–CTA06; conciliación al centavo y rastreo bidireccional de documento a mayor.

### S13. Completar el esquema y las migraciones de integridad

**Archivos:** `B/Database/ApplicationDbContext.cs`, entidades y `B/Migrations/`; DTO/repositorios relacionados.

1. Crear migraciones aditivas para autorizaciones, instantáneas, componentes tributarios, pagos/aplicaciones, períodos, idempotencia, cola e historia. No editar la migración inicial ya utilizada.
2. Fijar precisión monetaria, tasas y cantidades; agregar claves foráneas sin cascada destructiva, unicidades de origen/correlativo/documento de compra y checks de importes/fechas/estados.
3. Evitar que constraints de importes positivos impidan representar reversos: el signo pertenece al evento/documento y al asiento; las cantidades y magnitudes de sus líneas se mantienen válidas.
4. Detectar y aislar datos anteriores inválidos antes de agregar constraints. Generar informe de filas rechazadas y plan de corrección; nunca descartarlas con `DELETE` para que pase la migración.
5. Validar índices parciales/exclusión para reservas activas y restricciones de stock en PostgreSQL real. Resolver el diseño exacto con los planes de consulta de S27.
6. Ensayar instalación limpia y actualización desde copia de la versión auditada, en la versión principal elegida para producción. PostgreSQL 18.6 probado en auditoría no demuestra compatibilidad operacional con 16.

**Salida:** DAT04–DAT07, CON01–CON05 y M01–M03. Cada migración tiene precondiciones, postcondiciones, respaldo y recuperación descritos.

### S14. Separar reservas, disponibilidad y estado físico de habitaciones

**Archivos:** `B/Controllers/{Reservations,Rooms}Controller.cs`, repositorios/entidades de reservas y habitaciones, `F/pages/reservations/ReservationsPage.tsx`, `F/pages/rooms/FloorMapPage.tsx`.

1. Definir estados y transiciones: solicitud/confirmada → ingresada → salida; cancelada y no-show con reglas propias. Validar transición, fechas, versión y permisos en servidor.
2. Separar limpieza/mantenimiento/ocupación actual de compromisos por intervalo. Una reserva futura no cambia la habitación actual a reservada ni impide vender noches anteriores disponibles.
3. Usar intervalos de estancia `[entrada, salida)` y restricción de no solapamiento para estados que bloquean cupo. Incluir cambios de habitación y de fechas; excluir reservas canceladas conforme a la política.
4. Validar capacidad por tipo de huésped y cuarto, bloqueo por mantenimiento y cambios concurrentes. La segunda de dos solicitudes incompatibles debe recibir 409 comprensible, no ocupar el mismo cuarto.
5. Reemplazar cascadas de borrado lógico por desactivación para uso futuro. Habitaciones, huéspedes/clientes y proveedores referenciados siguen visibles en historia según permisos.

**Salida:** HOT01–HOT05, CON03 y DAT06; disponibilidad y ocupación se comprueban con reservas adyacentes, futuras y simultáneas.

### S15. Unificar check-in, folio, anticipos y salida

**Archivos:** `B/Controllers/{Reservations,Folios}Controller.cs`, `B/Dtos/common/HotelDtos.cs`, entidades de reservas, `F/pages/reservations/{CheckInPage,CheckOutPage}.tsx`, `F/pages/folios/FoliosPage.tsx`.

1. El check-in recibe la reserva seleccionada y su versión; solo crea una reserva nueva cuando el operador elige explícitamente ingreso sin reserva. Un fallo al guardar huésped no permite avanzar como si hubiera éxito.
2. Crear folio y cargos con origen único: noche/servicio/producto. Guardar fechas de prestación, reglas y precio pactado; definir qué cambios requieren recotización autorizada.
3. Incorporar asignaciones de líneas de factura a cargos de folio. Cada cantidad/importe solo se factura una vez; el anticipo es una obligación/aplicación identificable, no un descuento adicional.
4. Aplicar la decisión fiscal sobre cuándo emitir por anticipos/servicios. No imponer que toda factura se emita al check-out si la obligación nace antes; tampoco duplicar la factura ya emitida al ingreso.
5. En salida, mostrar cargos, facturado, créditos, pagos, anticipos y saldo. Exigir saldo resuelto mediante pago o crédito empresarial autorizado, con cuenta por cobrar y vencimiento. No marcar como pagado un saldo a crédito.
6. No cerrar folio si quedan cargos sin tratamiento, como el caso auditado de L1,305 frente a L1,190 facturados. Cerrar habitación/folio y eventos financieros como un solo caso de uso consistente.

**Salida:** HOT06–HOT10, PAG04 y E2E01–E2E03. Repetir entrada/salida por timeout no crea otra estadía ni otra factura.

### S16. Registrar pagos, devoluciones y caja real

**Archivos:** entidades/DTO/repositorio/controlador de caja, facturas y folios; `F/pages/cash/CashRegistersPage.tsx`; servicios de pagos y conciliación **(nuevos)**.

1. Crear pago con método, moneda, importe, referencia, estado y fecha; aplicaciones a documentos/anticipos, movimientos de caja y asiento. Un estado `Pagada` no sustituye estos registros.
2. Calcular cambio en servidor exclusivamente para efectivo; rechazar recibido insuficiente salvo pago parcial explícito. El total cobrado por varios medios debe coincidir con las aplicaciones y el saldo.
3. Exigir caja/turno abierto para efectivo; permitir abonos y reembolsos vinculados. Evitar pagos huérfanos y aplicación acumulada superior al saldo bajo concurrencia.
4. Calcular esperado = apertura + entradas de efectivo − salidas de efectivo. El usuario ingresa contado y explicación, nunca el esperado. Tarjetas/transferencias se concilian fuera del efectivo.
5. Modelar liquidación de tarjeta: importe bruto, comisión, retención sufrida, depósito neto y comprobante. No registrar solo el depósito como ingreso por alojamiento.
6. Cierre/reapertura requieren control de versión, permiso y auditoría; conservar arqueo por denominación si se adopta. Corregir o crear la ruta de movimientos actualmente ausente y eliminar errores silenciados.

**Salida:** PAG01–PAG08, CON04 y CTA03. El caso apertura 1,000 y contado 1 produce diferencia −999 si no hay movimientos, aunque el cliente envíe esperado 1.

### S17. Completar compras, proveedores y cuentas por pagar

**Archivos:** `B/Controllers/{PurchaseInvoices,Suppliers}Controller.cs`, `B/Database/Repositories/PurchaseRepository.cs`, entidades de proveedor y DTO de compra; pantallas de compras/proveedores **(nuevas si no existen)**.

1. Registrar documento del proveedor con identidad, RTN, tipo, número, CAI/rango/fecha cuando corresponda, fecha contable, moneda, líneas y respaldo. Documentar verificación de validez para crédito/costo deducible.
2. Evitar duplicados por proveedor y clave fiscal completa, considerando tipos de comprobante y normalización. Una advertencia visual no sustituye índice/validación concurrente.
3. Clasificar cada línea como gasto, inventario o activo; separar ISV acreditable, no acreditable y sujeto a prorrata aprobada. No enviar toda compra a 5109 ni reconocer automáticamente todo impuesto como crédito.
4. Registrar abono/pago: Debe proveedor, Haber banco/caja y retenciones si proceden; actualizar saldo desde aplicaciones. Pagar no puede limitarse a cambiar un enum.
5. Registrar notas/devoluciones del proveedor y reversión de recepción/valoración cuando aplique. Prohibir borrar compras contabilizadas; corregir por documento y asiento vinculados.
6. Añadir UI completa para captura, búsqueda, validación, abonos y consulta de saldo, con permisos y errores; enlazar rutas y navegación.

**Salida:** COM01–COM07 y CTA04. Mayor de proveedores = auxiliar; ninguna línea de asiento queda huérfana al corregir un documento.

### S18. Añadir retenciones y auxiliares tributarios aplicables

**Archivos:** `B/Controllers/{Reports,PurchaseInvoices,DataExport}Controller.cs`, servicios contables; entidades `WithholdingEntities.cs`, servicio `WithholdingService.cs` y UI de retenciones **(nuevos)**.

1. Activar solo obligaciones confirmadas en DEC-07. Separar retenciones practicadas a proveedores, retenciones sufridas y tasa turística; cada una tiene base, momento, cuenta y declaración propios.
2. Guardar retenido/RTN, operación origen, tipo/código, base, tarifa vigente, importe, fecha, comprobante y sustento de excepción. Conservar constancia y vínculo a pago/documento.
3. Implementar comprobante de retención cuando aplique, con autorización, formato y datos correspondientes. No reutilizar el rango de facturas ni fingir que un recibo interno cumple ese requisito.
4. Registrar asiento: deuda al proveedor se extingue entre pago neto y pasivo por retención; entero posterior cancela ese pasivo. Retenciones sufridas crean el activo/crédito correspondiente, no gasto automático.
5. Importar auxiliares de nómina/activos/ISR si esos módulos se operan fuera del ERP: contrato de columnas, validación, duplicados, autorización y conciliación. Si se exige cálculo dentro del ERP, convertirlo en subalcance obligatorio antes de declarar esa obligación cubierta.
6. Marcar no aplicabilidad con motivo firmado; no mostrar formularios vacíos como declaraciones cumplidas.

**Salida:** RET01–RET05 y N03. Si aplica al hotel, el módulo no puede diferirse y al mismo tiempo declararse cumplimiento fiscal completo.

### S19. Integrar kardex, costos y consumo

**Archivos:** `B/Controllers/InventoryController.cs`, `B/Database/Entities/ProductEntities.cs`, DTO inventario, compra/folio/emisión y `F/pages/inventory/InventoryPage.tsx`.

1. Eliminar edición libre de stock. Crear entradas, salidas, ajustes, devoluciones y saldos iniciales, con cantidad, costo, origen, fecha, usuario y motivo.
2. Aprobar método de valoración y cuentas con el contador. Incorporar recepciones de compra, consumos de habitación/venta y costo de venta; no confundir valor de inventario con precio de venta.
3. Vincular cada movimiento a línea de compra, cargo o documento; impedir doble descuento por generar factura de un consumo ya registrado en folio.
4. Usar actualización atómica condicionada a existencia suficiente o bloqueo de fila. No permitir stock negativo salvo una política excepcional explícita que incluya valoración y autorización; la política propuesta inicial lo prohíbe.
5. Ajustes de conteo físico requieren aprobación y contrapartida contable; reversar movimientos con trazabilidad. Mantener costo histórico al cambiar el precio del producto.

**Salida:** INV01–INV06 y CON05. Dos salidas de 7 desde stock 10 producen una aceptada y una rechazada, stock 3 y kardex de salida 7.

### S20. Implementar períodos y estados contables confiables

**Archivos:** `B/Controllers/AccountingController.cs`, repositorio/servicio contable, entidades contables y `F/pages/accounting/`.

1. Crear períodos contables con apertura/cierre, fecha, responsable y reapertura autorizada. Validar en servidor todas las operaciones que contabilizan; cerrar solo desde UI no sirve.
2. Impedir asientos en cuentas agrupadoras o inactivas para nuevas operaciones; conservar cuentas inactivas en mayor y balances históricos. Restringir cambios de tipo/naturaleza después de su uso.
3. Consolidar cuentas padre desde descendientes sin sumar dos veces. Calcular saldos iniciales, movimientos del período y saldo final, con tratamiento de activo/pasivo/patrimonio/ingresos/gastos.
4. Hacer explícito el ejercicio y la política de resultado/traspaso. Un reporte titulado anual filtra el año; no utiliza toda la historia.
5. Conciliar cuentas por cobrar, pagar, caja, bancos, impuestos e inventarios antes del cierre. Registrar ajustes autorizados; no ocultar diferencias en una cuenta genérica.
6. Los ajustes posteriores conservan fecha de registro y período aplicable; la rectificación fiscal es un trámite distinto de reabrir contabilidad.

**Salida:** CTA05–CTA10 y DAT02. Un cierre debe impedir también escrituras concurrentes iniciadas antes de su confirmación, mediante revalidación/bloqueo en transacción.

### S21. Reconstruir libros, exportaciones y declaraciones

**Archivos:** `B/Controllers/{Reports,DataExport,Dashboard}Controller.cs`, consultas contables, `F/pages/reports/ReportsPage.tsx`, `F/pages/dashboard/DashboardPage.tsx`.

1. Rehacer libro de ventas desde documentos inmutables: identificar facturas, créditos, débitos y anulaciones por su naturaleza; conservar filas y aplicar el efecto correcto sin duplicar reversos.
2. Separar ventas/devengo de cobros. El dashboard debe indicar período, criterio y última actualización; no rotular movimiento manual como tiempo real garantizado.
3. Crear auxiliares de ISV por tasa/base, turismo, compras y retenciones, enlazando cada cifra con los documentos que la componen. Conciliar con cuentas de control antes de exportar.
4. Sustituir el actual «DMR-1» derivado del ISV. Implementar DMC, declaración ISV, turismo 259 y retenciones que correspondan según perfil y versión oficial de formulario. Usar casillas/códigos documentados; no conservar 30/40/55/70 sin validación.
5. Definir exportaciones en texto y formatos requeridos, con orden de columnas, codificación, fechas, decimales, catálogos, validación y totales de control. Distinguir exportación auxiliar de archivo oficialmente importable.
6. Crear estados preparado → revisado → presentado → pagado, con acuse/boletín/comprobante adjunto y versionado de rectificaciones. Generar un archivo no equivale a presentarlo al SAR.
7. Mantener calendario de obligaciones con reglas y excepciones vigentes revisadas por el contador. No programar una prórroga histórica como vencimiento permanente.

**Salida:** REP01–REP08 y N04. Archivo de muestra comparado con plantilla vigente y aceptado por el mecanismo oficial disponible o documentado como auxiliar para captura supervisada.

### S22. Reparar y ampliar la bitácora

**Archivos:** `B/Services/AuditService.cs`, entidades de autenticación/auditoría, `B/Controllers/AuditLogsController.cs`, todos los casos de uso sensibles y UI de bitácora.

1. Canonicalizar fechas a precisión persistida en PostgreSQL, importes a representación decimal estable y campos en orden fijo. Guardar versión del algoritmo; verificar registros sin depender de cultura ni zona del servidor.
2. Serializar actualización del último hash o usar una secuencia protegida; cubrir 50 eventos simultáneos. Generar evento en la misma transacción de cada cambio financiero autorizado.
3. Incluir creación/cambio de reglas, permisos, emisión, notas, pagos, exportaciones, backup, cierres, migraciones y fallos relevantes, con actor real, motivo y correlación. Redactar secretos/PII innecesaria.
4. Limitar permisos de escritura y prohibir UPDATE/DELETE ordinario sobre eventos. Mantener comprobación/exportación y anclas fuera de la misma base bajo custodia separada.
5. Documentar la limitación frente a administradores de máquina/base; un hash local recalculable no hace la bitácora absolutamente inmutable. Cambiar mensajes de UI para describir la garantía real.
6. Conservar cadena anterior con su versión y anomalías; no recalcularla para borrar evidencia del defecto original sin un procedimiento de migración documentado.

**Salida:** AUD01–AUD05; cadena intacta verifica tras reinicio/restore, manipulación se detecta y concurrencia no produce bifurcaciones.

### S23. Emitir representaciones fieles e imprimir en Windows

**Archivos:** `B/Services/{EscPosService,RawPrinterHelper}.cs`, `B/Controllers/PrintController.cs`, configuración de impresión, `F/pages/settings/SettingsPage.tsx`.

1. Crear un modelo de representación común desde la instantánea emitida; plantillas por tipo de documento, con bloques fiscales obligatorios que la configuración no pueda ocultar.
2. Incorporar datos del emisor/adquirente, CAI y rango reales, fecha límite, desglose, moneda, total en letras cuando corresponda, notas y referencias; original/copia y conservación conforme al formato aplicable. Las fechas de estancia provienen del folio/reserva correcta.
3. Generar PDF/archivo conservable y salida térmica desde ese modelo. Mantener original y copia fieles, versión de plantilla y huella; no cambiar la factura histórica al actualizar ajustes del hotel.
4. Añadir `IPrinterTransport` y adaptador Windows **(nuevos)**; aislar llamadas Winspool y cualquier dependencia `System.Drawing`. Enumerar solo impresoras autorizadas, devolver error controlado en plataformas no soportadas y tratar colas/atascos sin perder documentos.
5. Probar físicamente en Windows 11 con la identidad que ejecutará el servicio y el modelo real de impresora: márgenes, acentos, ñ, números largos, corte, varias páginas, original/copia y reimpresión. No asumir que Windows del host vuelve compatibles las APIs dentro de un futuro contenedor Linux.
6. Guardar ID de trabajo, intentos y resultado; distinguir enviado a cola de físicamente entregado. La reimpresión no genera otro correlativo. Vista previa usa todos los cambios sin guardar y lleva marca de muestra cuando no es documento emitido.
7. Si se usa papel térmico, completar el expediente del proveedor y trámite aplicable; no confundir garantía de papel con plazo general de custodia fiscal.

**Salida:** IMP01–IMP07 y N05; aprobación de muestras por contador/recepción y evidencia física. El transporte desde contenedores se valida después en D02.

### S24. Reparar flujos frontend contra contratos definitivos

**Archivos:** `F/pages/invoices/`, `F/pages/reservations/`, `F/pages/folios/`, `F/pages/cash/`, `F/lib/axios.ts`, tipos y rutas.

1. Corregir método/ruta de nota de crédito, `FolioId` de cargos, fechas `YYYY-MM-DD`, enum de comprador y filtro de autorizaciones. Crear una capa API tipada, eliminando cadenas inconsistentes dispersas.
2. Descargar PDF con petición autenticada y objeto temporal revocado después; no usar `window.open` a una ruta que exige Bearer sin mecanismo válido.
3. Mantener reserva/huésped seleccionados durante el flujo, limpiar estado al cambiar de operación y evitar valores antiguos entre clientes.
4. Deshabilitar confirmación mientras hay envío, usar clave de idempotencia por intención y mostrar resultado del servidor. Un retry conserva la clave; una operación nueva crea otra.
5. Mostrar cargos, impuesto, anticipo, saldo y cambio calculados por API. Separar previsualización de emisión irreversible y mostrar número definitivo solamente tras confirmar.
6. Implementar compra, proveedor, retención o auxiliares requeridos por el perfil con navegación y permisos completos; no dar por entregado un módulo únicamente porque existe el endpoint.

**Salida:** UI01–UI06 y E2E01–E2E05; ninguna acción visible termina sistemáticamente en 400/405 o en éxito falso.

### S25. Completar errores, navegación, accesibilidad y diseño

**Archivos:** `F/components/layout/`, `F/components/ui/`, `F/pages/`, `F/index.css`, `F/main.tsx`, `F/App.tsx`.

1. Separar estados de carga, sin resultados, sin permisos, error y éxito; preservar datos del formulario tras fallo. Evitar que un timeout muestre caja cero o «guardado».
2. Unificar etiquetas enlazadas, nombres accesibles de iconos, errores junto al campo y foco al primer error. Combobox por teclado, modales con foco contenido/restaurado y Escape conforme al contexto.
3. Adaptar sidebar y tablas a 390 px y escritorio; desplazamiento local en tablas amplias, sin desbordamiento global ni acciones inaccesibles.
4. Corregir contraste usando los hallazgos de la revisión con impeccable; definir tokens, estados con texto/icono además de color y estilos de foco. Revisar tema y zoom sin perder cifras.
5. Alinear rutas, breadcrumbs, búsqueda rápida y menú con permisos; eliminar promesas y botones inertes. Implementar Enter en paleta o retirar indicación.
6. Alojar recursos esenciales localmente, respetar reducción de movimiento y añadir ErrorBoundary. Sustituir copias de «bitácora inmutable», «DMR listo» o «tiempo real» por estados comprobables.

**Salida:** UI07–UI13; revisión manual de teclado y tareas de recepción, además de análisis automatizado de accesibilidad.

### S26. Hacer segura la búsqueda y acotar la carga de datos

**Archivos:** listados/controladores, repositorios, `F/components/ui/Pagination.tsx`, páginas con búsquedas y `F/lib/axios.ts`.

1. Aplicar filtros, orden estable y paginación en servidor a facturas, folios, huéspedes, compras, productos, asientos y auditoría; límite máximo configurable y total/count cuando sea necesario.
2. Consultar DTO proyectados y solo campos requeridos. Detalle por ID trae líneas; la lista no transporta todos los hijos. Exportaciones usan flujo por lotes o trabajo persistente según volumen.
3. Debounce en búsquedas, cancelar solicitudes anteriores y descartar respuestas obsoletas. Reiniciar página al cambiar filtros y usar claves estables.
4. Añadir timeout y retry solo donde sea seguro; mutaciones financieras siguen S10. Evitar paginar únicamente los primeros 15 registros descargados.
5. Mantener autorización y criterios contables iguales en pantalla y exportación; informar filtros/período en archivos generados.

**Salida:** UI14–UI16, PERF01–PERF03; búsqueda rápida no muestra resultados de una consulta anterior.

### S27. Medir consultas, salud y arranque

**Archivos:** repositorios/consultas de reportes, `B/Database/ApplicationDbContext.cs`, migraciones de índices y `B/Program.cs`.

1. Sustituir N+1 por agregaciones SQL/proyecciones; revisar planes de consultas reales con volumen de referencia y parámetros representativos.
2. Crear índices alineados con filtros/rangos de fecha, claves foráneas, estado y origen; comparar coste de lectura y escritura. No indexar toda columna sin medición.
3. Medir p50/p95, tamaño de respuesta, consultas por petición, CPU/memoria y bloqueos con 10 usuarios simulados. El anexo fija objetivos iniciales de ingeniería, revisables según el hardware Windows acordado.
4. Separar liveness de readiness: comprobar conexión y esquema esperado, configuración imprescindible y versión. Estado sin DB debe indicar no preparado y evitar aceptar emisiones.
5. Tratar migraciones como paso controlado; no ocultar fallos para levantar una app parcialmente preparada. Registrar diagnóstico seguro y `traceId`.

**Salida:** PERF01–PERF05 y OPS01–OPS02. No se exige aquí configurar healthchecks de Compose.

### S28. Respaldo completo, procesos robustos y recuperación

**Archivos:** `B/Services/{DatabaseBackupService,BackupHostedService,BackupOptions}.cs`, `B/Controllers/BackupController.cs`, UI de respaldo y almacenamiento de adjuntos.

1. Respaldar PostgreSQL, adjuntos, documentos conservados, configuración necesaria y versiones de esquema/aplicación; guardar claves de recuperación mediante custodia separada y segura. No incluir secretos en un ZIP descargable por recepción.
2. Identificar cada trabajo/archivo con UUID y manifest de hashes, fechas y alcance. Descargar por ID autorizado; eliminar dependencia de «último respaldo global» y nombres con precisión de segundos.
3. Ejecutar `pg_dump`/restore con argumentos estructurados, rutas Windows con espacios, timeout, cancelación, lectura de stderr y código de salida; impedir ejecución de shell por nombres aportados por el usuario.
4. Serializar trabajos incompatibles, aplicar retención configurable y verificar integridad antes de borrar respaldos anteriores. Detectar disco lleno, destino desconectado y tarea atrasada.
5. Guardar copia cifrada fuera del único disco/equipo, con acceso probado a las claves. Acordar RPO/RTO y frecuencia; una copia en el mismo SSD no cubre su pérdida.
6. Ensayar restauración aislada y reconciliar números emitidos después del último backup antes de volver a facturar. No reiniciar correlativos desde una copia atrasada que desconozca documentos ya entregados.
7. Documentar contingencia autorizada ante corte de energía/impresora/servidor y posterior registro sin duplicar ventas. La solución de contingencia fiscal se valida en DEC-11.

**Salida:** OPS03–OPS08, M04 y SEC05; recuperación cronometrada en equipo alterno, sin depender de Docker para esta prueba.

### S29. Migrar datos y preparar el corte

**Archivos:** nuevas migraciones y utilidades de importación, `B/Migrations/`, datos heredados; manual de migración **(nuevo en docs)**.

1. Identificar origen real de cada base. No asumir que la SQLite versionada contiene historia productiva ni que convertir el esquema importa datos automáticamente.
2. Construir mapeo de IDs, fechas, cuentas, autorizaciones, documentos y líneas. Guardar origen y huella de cada lote; separar inválidos para revisión.
3. Tratar documentos anteriores como historia: no recalcular impuestos, fabricar CAI ni modificar números. Si faltan instantáneas, reconstruir solo desde evidencia disponible, marcando procedencia y limitaciones; nunca inventar datos.
4. Conciliar conteos, sumas por tipo/período/tasa, Debe/Haber, saldos, stock y últimos correlativos. Ajustes contables requieren aprobación; anomalías fiscales se tramitan según corresponda.
5. Ensayar importación repetible en copia, congelamiento de escrituras, respaldo de corte, migración, comprobación, cambio de acceso y recuperación. Una importación repetida no duplica registros.
6. Separar rollback técnico previo a nuevas emisiones de recuperación posterior: tras emitir, una vuelta a base antigua necesita preservar/reconciliar todas las nuevas operaciones; no restaura un backup a ciegas.

**Salida:** M01–M06. Acta de saldos y continuidad documental aprobada por contador y administración.

### S30. Ejecutar aceptación integral y capacitación

**Archivos:** pruebas E2E, manuales y actas en `docs/implementacion-sar/`; artefacto de release identificado.

1. Ejecutar todo el catálogo aplicable del anexo sobre una instalación limpia y sobre una copia migrada. Incluir fallos y concurrencia, no solo el camino exitoso.
2. Repetir la jornada hotelera completa: reserva, anticipo, ingreso, consumos mixtos, nota parcial, devolución, salida, compra, abonos, kardex, arqueo, libros y cierre.
3. Hacer revisión independiente de cifras con hoja/control firmado por contador, sin usar el mismo algoritmo del backend para generar resultados esperados.
4. Ensayar recepción sin ayuda de desarrollo: buscar reserva, recuperar cobro tras timeout, distinguir factura y recibo, reimprimir y explicar diferencia de caja. Registrar errores y tiempo por tarea.
5. Probar impresora Windows física, restauración, baja de usuario, cierre de período y continuidad de numeración. Archivar evidencias por SHA/versión de reglas.
6. Capacitar y entregar guías por rol, contingencia, respaldo y presentación fiscal. Registrar responsables suplentes y acceso a soportes sin compartir credenciales personales.

**Salida:** E2E01–E2E06, G0–G5 con evidencia; defectos pendientes vuelven a su paso, no se sustituyen por una firma de aceptación genérica.

### S31. Aprobar la versión y reunir el expediente fiscal

**Archivos:** checklist/actas, manifiesto de release, manuales, ficha fiscal y matriz de trazabilidad.

1. Verificar cierre de todos los P0/P1 del alcance funcional y de las obligaciones aplicables; resolver P2 o registrar excepción limitada, responsable, mitigación y vencimiento. Ninguna excepción debe afectar importe, autorización, custodia o seguridad crítica.
2. Firmar acta técnica (QA/revisor), acta contable/fiscal interna (contador) y operativa (hotel/Windows). Usar el formato del anexo, con evidencias y versiones exactas.
3. Adjuntar autorizaciones, inscripción/registro del sistema, declaración jurada, CAI/rangos y documentación de papel térmico si corresponde. Tramitar ante SAR lo pendiente por el canal aplicable; registrar resultado real, no presumirlo.
4. Verificar vigencia normativa y perfil nuevamente antes del primer documento real. Un acuse de presentación no certifica la exactitud del contenido tributario.
5. Publicar release reproducible, registro de cambios y condiciones de uso. Marcar expresamente: **aceptación funcional local conseguida** solo si las pruebas pasan; **despliegue Docker pendiente** hasta GD.
6. Revisar conciliación diariamente durante las primeras 5 jornadas operativas y cerrar el primer mes con el contador. Son controles a ejecutar en la implementación, no una automatización creada por este plan.

**Salida G6:** expediente completo del alcance local; aprobación externa registrada solo cuando exista. No afirmar cumplimiento total si queda una obligación aplicable sin implementación o procedimiento aceptado.

## 4. Trabajo de contenerización diferido

### D01. Empaquetado, proxy, red y persistencia

OPS-02 y OPS-03 quedan **diferidos**, no corregidos. Al retomar: arreglar rutas COPY/build del backend, publicación del frontend desde fuente, fallback SPA, conservación del prefijo `/api`, configuración segura de proxy/TLS, credenciales, puertos, volúmenes, healthchecks y proceso de migración. Revalidar la parte de SEG-08 relativa al despliegue. La entrega de S31 no permite saltar estas comprobaciones para operar en Docker.

### D02. Windows 11, Docker e impresión real del conjunto

Decidir tipo de contenedor y transporte de impresión: adaptador dentro de entorno compatible o puente Windows autenticado y restringido, según arquitectura elegida. Probar acceso a impresoras, permisos, volúmenes, reinicio de Windows/Docker, recuperación y LAN. Repetir smoke de emisión, nota, cobro, impresión, rutas y backup desde el artefacto final. La prueba nativa de Winspool en S23 no acredita su funcionamiento en Linux.

**Puerta GD:** instalación completa desde cero en el Windows 11 destino, acceso desde clientes del hotel, recarga de rutas, desconexión/reinicio y recuperación sin pérdida ni duplicación. Su ejecución queda expresamente fuera de esta etapa por indicación del usuario.

## 5. Regla de terminado para cada corrección

Una tarea pasa de Pendiente a Implementada cuando el código y la migración existen; pasa a Verificada cuando tiene resultados reproducibles y revisión independiente; pasa a Aprobada cuando el responsable de su puerta firma la evidencia. No usar «aprobado» para una tarea solamente diseñada.

Cada entrega debe adjuntar: ID de tarea/hallazgo, problema y comportamiento final, archivos cambiados, reglas fiscales involucradas, compatibilidad/migración, pruebas ejecutadas con resultado, limitaciones, evidencia y revisor. Las nuevas rutas/columnas se documentan en el contrato; las reglas aprobadas reciben versión y fecha de vigencia.

El cierre general exige que las cifras del comprobante, libro, asiento, auxiliar y declaración aplicable se puedan reconstruir y conciliar; que un fallo no deje operaciones parciales; y que el personal pueda completar una jornada y recuperar el sistema. La conformidad se limita al perfil fiscal, versión y entorno efectivamente verificados.
