# Registro de ejecución

## 2026-09-07 — inicio de implementación

- Rama inicial: `master`.
- Commit base: `8e5bf6c603fb16519ef7652b0dbb610ac7e1c9de`.
- Se conservaron diez modificaciones preexistentes bajo `backend/src/hotel-erp.Api/obj/` y los documentos no versionados de auditoría/plan.
- Backend: `dotnet build hotel-erp.slnx --no-restore` finalizó con 0 errores y 10 advertencias. Una advertencia nullable afecta `InvoiceRepository`; ocho son compatibilidad Windows de imagen/impresión; una es referencia potencialmente redundante de CodePages.
- Frontend: `npm run build` falló con 7 diagnósticos sintácticos en `ChartOfAccountsPage.tsx` y `CheckInPage.tsx`.
- Esta entrada es evidencia de línea base, no aprobación Q01/Q02.

## 2026-09-07 — lote 1: build, autorización y sesiones

### Entorno y cadena reproducible

- Versiones efectivas: .NET SDK `10.0.302`, runtime `10.0.10`, Node `26.7.0`, npm `11.19.0`, cliente `psql 18.6`; integración ejecutada sobre PostgreSQL `16-alpine` aislado en `127.0.0.1:55432` con datos y credenciales sintéticas.
- Frontend normalizado a npm: se conservan `package.json` y `package-lock.json`; se retiró `pnpm-lock.yaml`.
- `npm audit --audit-level=low`: 0 vulnerabilidades conocidas.
- `npm run lint`: 0 errores y 0 advertencias.
- `npm run build`: TypeScript y Vite aprobaron; 2.095 módulos transformados y bundle de producción generado.
- `dotnet test hotel-erp.slnx --no-restore --nologo`: 34 aprobadas, 0 fallidas, 0 omitidas. Incluye cálculo monetario, políticas, atributos de endpoints y política de contraseña.
- La compilación realizada por la ejecución de pruebas terminó con 0 errores; el build completo anterior del mismo lote terminó con 0 advertencias.

### PostgreSQL, migración y catálogo de seguridad

- La migración `HardenAuthorizationAndSessions` se aplicó desde una base vacía y creó índices/columnas para nombre normalizado, identidad estable de rol, cambio obligatorio y versión de seguridad.
- El arranque sembró 41 cuentas contables, el catálogo estable de permisos y los roles del sistema. El administrador de QA se creó únicamente desde variables de entorno y quedó obligado a cambiar contraseña.
- Se verificó que el rol `Admin` con identidad de sistema no admite cambio de nombre ni eliminación: ambos intentos devolvieron 400.

### Matriz HTTP autenticada

Se ejecutaron 25 comprobaciones sobre la API real y PostgreSQL. Resultado final: `MATRIZ_HTTP_OK`.

| Caso | Resultado observado |
|---|---|
| Salud / usuarios anónimos | 200 / 401 |
| Login con clave inicial y `/api/auth/me` | 200; `mustChangePassword=true` |
| Habitaciones y usuarios antes del cambio | 403 con problema “Cambio de contraseña requerido” |
| Cambio de contraseña | 200 |
| Access token anterior y clave anterior | 401 / 401 |
| Login administrador renovado; usuarios y roles | 200 / 200 / 200 |
| Usuario Recepción con clave temporal | 200 al login; 403 al intentar habitaciones antes del cambio |
| Recepción tras cambio y nueva sesión | habitaciones 200; usuarios, roles, respaldos y reportes 403 |
| CORS `http://localhost:5173` | preflight 204 con origen permitido |
| CORS `https://evil.example` | preflight sin `Access-Control-Allow-Origin` |

Una segunda matriz terminó con `MATRIZ_SESION_OK`: contraseña débil 400, reutilización de contraseña 400, logout 200, access token anterior 401 y refresh token anterior 401.

### Interfaz de primer acceso

- El login dirige a `/change-password` cuando el backend marca contraseña temporal.
- La ruta protegida impide abrir `/dashboard` hasta completar el cambio.
- La pantalla presenta requisitos visibles, confirmación, errores accesibles y opción de cerrar sesión. Al cambiar la contraseña, limpia la sesión local y exige un login nuevo porque el backend revoca los tokens anteriores.
- Revisión manual en navegador local: escritorio 1280 px y viewport móvil 390 × 844; árbol accesible completo y consola sin errores/advertencias.
- Detector Impeccable ejecutado una sola vez al cierre del lote sobre las seis unidades UI cambiadas: `[]`, sin hallazgos.

### Límites de esta evidencia

- La prueba usó exclusivamente identidades y datos sintéticos; no acredita configuración fiscal real ni aprobación del SAR.
- Continúan abiertas `DAT-I01` y `PERF-I01` en [incidencias](../incidencias.md).
- No se marca la puerta G1: en este punto aún faltaban integración automatizada, perfiles Caja/Contador, instalación limpia, pruebas E2E y revisión técnica independiente. El lote siguiente añadió integración e instalación limpia.

## 2026-09-07 — lote 2: integridad, rotación e integración automatizada

- Se eliminó la cascada de borrado lógico que propagaba la baja de datos maestros hacia facturas, compras, reservas y folios. Esta conducta podía sacar documentos históricos de consultas y reportes sin una reversión fiscal.
- `Folio` y `FolioItem` tienen filtros coherentes; el repositorio de folios históricos carga de forma explícita huéspedes retirados y separa las colecciones para conservar el documento y evitar productos cartesianos.
- `UserRepository.GetByIdAsync` usa consulta dividida. Un arranque y login posterior sobre PostgreSQL no repitieron las advertencias `DAT-I01`/`PERF-I01`.
- Los refresh tokens se consumen mediante actualización condicional. Si se reutiliza uno ya consumido, se incrementa la versión de seguridad y se revocan los tokens activos de la cuenta.
- Se creó `hotel-erp.IntegrationTests`. La prueba usa `HOTEL_ERP_TEST_ADMIN_CONNECTION`, crea una base `hotel_erp_it_<uuid>`, aplica las migraciones, ejecuta los flujos y elimina la base aun cuando haya error. Después de la ejecución se verificó que quedaban 0 bases con ese prefijo.
- Integración aprobada: 1 prueba, 0 fallidas, 0 omitidas. Cubre migración desde cero, bootstrap, cambio obligatorio, 401/403, logout, replay de refresh token y conservación del folio tras borrar lógicamente al huésped.
- Se añadieron [scripts/qa.sh](../../../scripts/qa.sh) y [scripts/qa.ps1](../../../scripts/qa.ps1), con instrucciones en [ejecución local](../ejecucion-qa-local.md).
- Ejecución completa de `scripts/qa.sh` con PostgreSQL: restore correcto, build con 0 advertencias/errores, 34 unitarias aprobadas, 1 integración aprobada, `npm ci`, auditoría con 0 vulnerabilidades, lint, build de 2.095 módulos y `git diff --check`; código final 0.

## 2026-09-07 — lote 3: perfil fiscal, atomicidad e idempotencia

### Perfil fiscal operable

- Se añadieron estado persistente `Borrador/Aprobado/Retirado`, versión, vigencia, actor, instante y motivo/nota mediante la migración `AddFiscalProfileApproval`.
- Una instalación nueva deja razón social, RTN, dirección y tasas sin valores ficticios. La migración limpia únicamente la combinación exacta de la antigua semilla demostrativa.
- La emisión devuelve 409 mientras el perfil está incompleto, en borrador, retirado, aún no vigente o vencido. La aprobación valida nombre, RTN de 14 dígitos, dirección, tasas y rango de fechas.
- Configuración expone el ciclo Borrador → Aprobado → Retirado. El recorrido de QA guardó un perfil sintético, incrementó su versión a 2 y lo aprobó desde la UI; cada transición produjo su auditoría.
- DEC-01–DEC-15 y G0 siguen pendientes: esta prueba no sustituye los documentos reales ni la aprobación del contador o del hotel.

### Transacción fiscal e idempotencia

- `AddFinancialIdempotency` crea un registro permanente con índice único `UserId + Scope + Key`, hash SHA-256 de la solicitud y recurso resultante.
- `POST /api/invoices` exige `Idempotency-Key` UUID. Dentro de una transacción PostgreSQL toma un advisory lock por usuario/operación/clave, busca el intento, asigna correlativo, guarda factura, asiento, auditoría y resultado idempotente, y luego confirma.
- La misma clave con el mismo cuerpo devuelve 201 y el mismo ID aun si el perfil fue retirado después; la misma clave con cuerpo diferente devuelve 409. Falta extender este patrón a notas, caja, compras, inventario y transiciones hoteleras.
- Checkout conserva la clave por reservación en `sessionStorage` ante errores de red o fallos posteriores a la factura. Además envía `Gravado` para comprador con RTN, registra el método de pago y ya no inventa una dirección.
- La cadena de auditoría se serializa con lock transaccional para evitar bifurcaciones concurrentes. Queda pendiente la carga de 50 eventos y la resistencia ante manipulación privilegiada.

### Correlativos y documento

- CAI y autorización por tipo usan lectura fresca dentro del lock, vigencia de Honduras inclusiva y límite final. Cuando `CurrentCorrelative == InitialRange` y no hay documentos previos, el primer número emitido es exactamente el inicial.
- La integración creó una autorización sintética inicializada en `001-001-01-00000001` y confirmó que la primera factura usa ese mismo número.
- La ruta general de creación acepta únicamente `Factura`; las notas deben usar sus rutas específicas. Facturas, notas de crédito y notas de débito emitidas se rechazan en la ruta de edición.
- El snapshot y hash fiscal se crean antes de confirmar, pero la versión canónica del formato, límites acumulados de crédito y asientos correctos de notas continúan abiertos.

### Frontend y revisión visual con Impeccable

- Configuración muestra estado y versión fiscal, vigencia, nota de aprobación, motivo de retiro, permisos, campos faltantes y advertencia de cambios sin guardar.
- El sidebar móvil dejó de reservar ancho fijo: a 390 px permanece oculto y se abre como panel superpuesto con fondo de bloqueo; el contenido usa una sola columna y conserva acciones legibles.
- En un host no Windows, `GET /api/print/printers` devuelve 501 con un mensaje de plataforma claro en vez de lanzar `DllNotFoundException`; la impresión física sigue pendiente de prueba en Windows 11.
- Revisión visual: escritorio 1280 px y marco real de iframe de 390 × 844 px. Se comprobó apertura/cierre del menú y el ciclo de aprobación. No se recorrieron aún todos los módulos ni zoom 200 %.
- El detector de Impeccable se ejecutó una vez sobre el lote y señaló un acento lateral `border-l-4` en Checkout; se retiró y no se repitió el detector.

### PostgreSQL y QA

- Migraciones aplicadas en orden: `HardenAuthorizationAndSessions`, `AddFiscalProfileApproval` y `AddFinancialIdempotency`. `dotnet ef migrations has-pending-model-changes` confirmó que no hay cambios de modelo sin migración.
- La integración crea y elimina una base PostgreSQL 16 por ejecución; al terminar quedaron 0 bases `hotel_erp_it_%`.
- Resultado final de `scripts/qa.sh`: restore y build con 0 advertencias/errores; 36 unitarias y 1 integración aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; lint limpio; bundle de 2.095 módulos; validación de espacios/conflictos aprobada.
- La integración cubre base nueva, bootstrap/cambio de contraseña, 401/403, rotación y replay de refresh token, borrado lógico histórico, ciclo fiscal, primera numeración, factura de L119, partida doble, hash, auditoría, retiro e idempotencia.
- La impresión física sigue pendiente de Windows 11 y del equipo real. En plataformas distintas de Windows, los endpoints de spooler responden 501 con un mensaje controlado en vez de cargar `winspool.drv`.

## 2026-09-08 — lote 4: motor monetario y notas fiscales

### Cálculo monetario autoritativo

- `InvoiceCalculationService` calcula en servidor cada línea, descuento, base e impuesto. Redondea a dos decimales con `MidpointRounding.AwayFromZero` y suma los resultados ya redondeados.
- El resultado separa ISV 15 %, ISV 18 %, base gravada, exenta y exonerada. El cliente ya no puede imponer `LineTotal` a una nota de crédito: solo identifica la línea original y su cantidad.
- Se rechazan documentos sin líneas o con total cero, cantidades no positivas, precios negativos, descuentos fuera de 0–100 % y tasas distintas de 0 %, 15 % o 18 %. Una línea declarada gravada no puede usar tasa cero.
- Se añadieron siete casos al proyecto unitario. El total pasó de 36 a 43 pruebas; incluye un oráculo mixto con descuento, tasa turística, ISV 15 %, ISV 18 % y línea exenta.

### Notas de crédito y débito

- Ambas rutas exigen `Idempotency-Key`, validan que el ID de la ruta coincida con el documento original y requieren una autorización activa específica para `NotaCredito` o `NotaDebito`.
- La nota de crédito se vincula mediante `InvoiceItem.OriginalInvoiceItemId`. Un advisory lock por factura serializa ajustes concurrentes y el servidor suma cantidades acreditadas antes de aceptar otra nota. Al agotar una línea responde 409; la factura cambia a `Anulada` únicamente cuando todas sus líneas quedaron acreditadas.
- La nota de débito aumenta cuentas por cobrar. En ese lote la nota de crédito ya debitaba ingreso, ISV y tasa turística y dejaba de simular caja; el lote 8 corrigió además la distribución entre cuentas por cobrar y saldo a favor según lo efectivamente pagado.
- `AccountingEntries.ReferenceId` tiene índice único para asientos activos. La migración aborta con un mensaje explícito si encuentra duplicados históricos activos antes de crear el índice.
- La interfaz dejó de ofrecer edición o anulación directa de comprobantes emitidos, usa la ruta correcta, filtra autorizaciones por tipo y conserva una intención UUID en `sessionStorage` cuando la respuesta es incierta.
- La representación ESC/POS identifica factura, nota de crédito o nota de débito; usa CAI, rango y fecha límite del snapshot fiscal y presenta ISV 15 % e ISV 18 % por separado. Una prueba evita volver al CAI general del documento relacionado.

### Migración y pruebas

- Migración agregada y aplicada: `20260908171830_HardenInvoiceAdjustments`. Añade la referencia de línea original, su FK restrictiva, índice de consulta e índice único de origen contable activo.
- `dotnet ef migrations has-pending-model-changes --no-build`: sin cambios de modelo pendientes.
- La integración PostgreSQL emite factura, nota de débito L11.50 y nota de crédito L119.00; comprueba hash, auditoría, idempotencia, partida doble, cuentas esperadas, mismo ID al reintentar y 409 al exceder el crédito.
- Resultado final de `scripts/qa.sh`: build con 0 advertencias/errores; 43 unitarias y 1 integración aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; lint limpio; bundle de 2.095 módulos; validación de espacios y conflictos aprobada.
- G2 continúa abierta por estados de cobro separados, reglas fiscales versionadas, pagos, compras, inventario, inyección de fallos, concurrencia masiva y aprobación de los ejemplos por el contador.

## 2026-09-08 — lote 5: check-in y liquidación atómica de folio

### Ingreso sin doble facturación

- `POST /api/reservations/checkin` bloquea la reserva y la habitación dentro de una transacción PostgreSQL, vuelve a leer su estado, valida habitación, fechas, capacidad, superposición, folio previo y descuentos porcentuales.
- El check-in abre la estancia y un único folio con su cargo de hospedaje. Ya no consume CAI, genera factura ni crea un asiento antes de conocer todos los consumos.
- La interfaz sustituyó el cobro del check-in por una confirmación de apertura y explica que la factura se emitirá al liquidar. Los descuentos de monto fijo no se presentan en este flujo porque aún no existe una regla de aplicación aprobada.
- La integración lanzó dos solicitudes concurrentes para la misma reserva: observó un `200`, un `409`, una reserva en `CheckIn`, una habitación ocupada, un folio, un evento de auditoría y ninguna factura o asiento adicional.

### Cargos y salida en una sola transacción

- Agregar un cargo al folio usa el mismo lock que la liquidación, recalcula todas las líneas con el motor monetario, rechaza tasas/cantidades/precios inválidos y audita el cargo. Se corrigió el contrato que exigía erróneamente un `FolioId` duplicado dentro del cuerpo pese a recibirlo en la ruta.
- `Invoice.FolioId` vincula la factura final y `InvoiceItem.FolioItemId` asigna cada línea persistida. Ambos tienen FK restrictiva e índice único activo; el backend calcula desde el folio y no acepta importes enviados por el navegador.
- Emitir la factura de un folio confirma en una transacción el correlativo, snapshot/hash, líneas, asiento, auditoría, registro idempotente, movimiento de efectivo, cierre del folio/reserva y estado `Limpieza` de la habitación.
- El endpoint separado de checkout solo admite una factura que realmente liquide ese folio y es idempotente si la estancia ya estaba cerrada. La interfaz retiró “Liberar sin facturar” y dejó de encadenar solicitudes independientes para factura, salida y caja.
- La pantalla filtra CAI activos, descuentos porcentuales y cajas activas; usa `Gravado` como valor contractual, conserva una tasa cero legítima, limpia los datos al cambiar de huésped y captura O.C. Exenta, Constancia SEFIN, registro SAG y exoneración de cada impuesto.

### Caja, migración y pruebas

- El efectivo exige caja activa con un movimiento de apertura posterior al último cierre. El servidor valida el importe recibido, calcula el cambio y suma al saldo únicamente el total cobrado. Desde el lote 8 el `CashMovement` referencia al `Payment` que materializa el cobro.
- Migración agregada y aplicada: `20260908203553_AtomicFolioSettlement`. Añade vínculos folio–factura y cargo–línea, snapshot del descuento, FKs e índices únicos. Antes del índice de caja aborta con un diagnóstico explícito si existen referencias históricas activas duplicadas.
- La integración añadió un cargo de minibar, rechazó salida sin factura, liquidó dos líneas por L 142.00 con L 150.00 recibidos y L 8.00 de cambio, comprobó saldo de caja L 192.00 desde una apertura L 50.00, partida doble, hashes, vínculos, auditoría y cierre. Repetir la misma intención devolvió el mismo ID; una intención distinta sobre el folio cerrado devolvió `409`.
- Verificación final: build .NET con 0 advertencias/errores; 43 pruebas unitarias y 1 integración PostgreSQL aprobadas; `npm ci`, `npm audit` con 0 vulnerabilidades, lint limpio, bundle de 2.095 módulos, `git diff --check`, modelo EF sin cambios pendientes y 0 bases temporales remanentes.
- G3 permanece abierta por devoluciones, anticipos, salida a crédito, conciliación de tarjeta/banco, compras, inventario y aprobación operativa externa. Pagos/aplicaciones, pago parcial/mixto, máquina de estados y cierre de caja se completaron en lotes posteriores.

## 2026-09-08 — lote 6: máquina de estados de reservas

### Transiciones e integridad concurrente

- `Reservation.Version` es un token de concurrencia. Crear inicia en versión 1; editar, confirmar, cancelar, check-in y checkout incrementan la versión dentro de su transacción.
- Confirmar solo admite `Pendiente`; cancelar solo admite `Pendiente` o `Confirmada`; check-in solo admite `Pendiente` o `Confirmada`; checkout requiere `CheckIn` y folio abierto. Los reintentos de confirmar o cancelar el mismo estado son idempotentes.
- Una petición con versión obsoleta recibe `409` y la versión actual. Cancelar exige un motivo de 3–500 caracteres y lo conserva en la cadena de auditoría.
- `DELETE /api/reservations/{id}` ya no oculta historia operativa: indica que debe usarse la cancelación auditada.
- Crear y editar toman locks por agenda de habitación, vuelven a consultar sus datos y validan huésped, rango `[entrada, salida)`, superposición, capacidad y bloqueo físico antes de confirmar.

### Agenda, estado físico e interfaz

- Una reserva futura ya no cambia la habitación a `Reservada`. La disponibilidad temporal se obtiene de las reservas activas; el estado físico queda limitado a libre, ocupada, limpieza, mantenimiento o bloqueada.
- `HardenReservationLifecycle` añade la versión con valor histórico 1. También convierte el antiguo estado `Reservada` a `Ocupada` cuando existe un check-in abierto y a `Libre` en los demás casos.
- La fila de reservaciones navega con `reservationId`. Check-in carga huésped, habitación, fechas, ocupantes y versión de esa reserva; no crea una segunda reserva. Check-out también selecciona automáticamente la estancia indicada.
- La edición de huésped ya no avanza cuando el guardado falla. La cancelación presenta un campo obligatorio y deshabilita la confirmación mientras el motivo sea inválido.
- La prueba visual reveló que `FolioDto.Items` no recibía `Folio.FolioItems`. El mapeo explícito fue corregido y protegido mediante una lectura HTTP en integración.

### Pruebas y evidencia observada

- La integración lanza dos altas simultáneas con la misma habitación y fechas: una responde `201` y la otra `409`; persiste una sola agenda sin cambiar el cuarto de `Libre`.
- El mismo escenario rechaza anticipo sin aplicación contable, confirmación con versión incorrecta, edición y cancelación obsoletas, confirmación/check-in después de cancelar y borrado directo. Confirma un solo evento de confirmación, un solo evento de cancelación y versión final 3.
- El escenario de estancia confirma versiones 1 → 2 → 3, un único check-in concurrente, una línea visible en el DTO del folio y liquidación atómica posterior.
- Revisión en navegador contra PostgreSQL de QA: una reserva existente abrió su folio por L 2,380.00; checkout mostró `Hospedaje - QA-101 x 2 noches`, subtotal L 2,000.00, ISV L 300.00, tasa turística L 80.00 y total L 2,380.00. Otra reserva se canceló con motivo y cambió a `Cancelada`.
- Verificación del lote: build .NET con 0 advertencias/errores; 43 unitarias y 1 integración PostgreSQL aprobadas; ESLint limpio; TypeScript/Vite aprobados con 2.095 módulos; migración aplicada, modelo EF sin cambios pendientes y 0 bases temporales remanentes.
- G3 sigue abierta por anticipos, devoluciones, salida a crédito, conciliación bancaria/tarjeta, compras, inventario, pruebas E2E completas y aprobación de usuarios operativos/contador. Pagos parciales/mixtos y cierre de caja se completaron en los lotes 7 y 8.

## 2026-09-08 — lote 7: apertura, cierre y arqueo de caja

### Saldo autoritativo y protección concurrente

- `POST /api/cash-registers/{id}/open` y `close` exigen permiso de caja, una clave UUID de idempotencia y una transacción PostgreSQL. Ambas rutas usan el mismo advisory lock `cash-register:{id}` que el cobro de folios.
- La apertura rechaza una caja inactiva o ya abierta. El cierre rechaza una caja cerrada y obtiene el monto esperado del último movimiento persistido; el navegador ya no puede declarar ni alterar ese valor.
- El cierre almacena por separado `ExpectedAmount`, `CountedAmount`, `Difference` y `Notes`. Una diferencia distinta de cero requiere una explicación de 3–500 caracteres y queda tanto en el movimiento como en auditoría.
- Cada operación confirmada crea un solo movimiento, un solo evento de auditoría y un solo registro idempotente. Repetir la misma intención devuelve el mismo `MovementId`; reutilizar la clave con otro cuerpo devuelve `409`.
- El listado de cajas deriva `IsOpen`, `CurrentBalance` y fecha del último movimiento en el servidor. La consulta usa un índice por caja, fecha y creación; el nombre activo de caja queda protegido por índice único.

### Migración e interfaz operativa

- La migración `20260909040128_HardenCashRegisterClosure` añade los campos de arqueo, precisión `numeric(18,2)`, límites de texto, checks contra monto/saldo negativo e índices de consulta/unicidad.
- Antes de alterar datos, la migración aborta con un diagnóstico si encuentra nombres duplicados, textos fuera de límite, valores negativos, más de dos decimales o cifras fuera de rango. Los cierres históricos reconstruyen esperado, contado y diferencia desde la secuencia previa, quedan identificados como migrados y recuperan su representación antigua al hacer rollback.
- Se comprobó el ciclo `down → up` sobre PostgreSQL de QA y `has-pending-model-changes` confirmó que la instantánea coincide con el modelo.
- La pantalla muestra únicamente la acción válida para el estado actual, usa el saldo entregado por el servidor, calcula la diferencia en vivo, bloquea el envío sin motivo, conserva la clave idempotente ante una respuesta incierta y deshabilita controles durante la operación.
- Checkout solo ofrece cajas activas con turno abierto. El historial representa el cierre como evento neutral, muestra sistema/contado/diferencia/nota, singulariza los contadores y presenta fecha y hora en `America/Tegucigalpa`.

### Pruebas y evidencia observada

- La integración cubre clave ausente, apertura y cierre idempotentes, clave reutilizada con otro cuerpo, caja ya abierta/cerrada, diferencia sin explicación y un `expectedAmount` manipulado que el contrato ignora. Con apertura L 1,000.00 y contado L 999.00, el servidor devuelve esperado L 1,000.00 y diferencia L −1.00.
- Dos aperturas simultáneas con claves distintas producen un `200` y un `409`; dos cierres simultáneos repiten el mismo resultado. Persisten exactamente dos aperturas, dos cierres, cuatro intenciones idempotentes y dos auditorías de cada operación en las dos sesiones válidas.
- Recorrido visual contra PostgreSQL: se creó `Caja QA visual`, se abrió con L 100.00 y se cerró con L 99.00. La interfaz exigió `Faltante detectado en prueba visual` y después mostró saldo del sistema L 100.00, contado L 99.00, diferencia L −1.00, la nota y el cajero.
- Resultado final de `scripts/qa.sh`: build .NET con 0 advertencias/errores; 43 unitarias y 1 integración PostgreSQL aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite aprobado con 2.095 módulos; `git diff --check` aprobado. No quedaron bases temporales.
- PAG05 queda cubierto en código y prueba automatizada para el caso de saldo manipulado. Tras el lote 8, G3 permanece abierta por devoluciones, anticipos, liquidación de tarjeta, conciliación bancaria, compras, inventario y aprobación operativa externa.

## 2026-09-09 — lote 8: auxiliar de pagos y cartera

### Pago, aplicación y concurrencia

- Se añadieron `Payment` y `PaymentApplication` con número propio, usuario, fecha, estado, moneda HNL, método, referencia, efectivo recibido/cambio, caja y origen contable. La aplicación identifica factura e importe; el saldo se deriva de aplicaciones confirmadas y notas de crédito, no del texto histórico `Pagada`.
- `POST /api/payments` exige permiso de caja e `Idempotency-Key` UUID. Normaliza importes a centavos, exige que la suma aplicada coincida exactamente con el pago y valida los campos por medio: efectivo requiere caja abierta y suficiente recibido; tarjeta/transferencia requieren referencia y no afectan caja de efectivo.
- Pago y nota de crédito comparten el advisory lock `invoice-balance:{id}`. La API relee pagos y créditos bajo ese lock, rechaza documentos no cobrables y evita que dos operaciones consuman el mismo saldo. Los locks de varias facturas se toman por ID ordenado y el lock de caja usa el mismo nombre que apertura/cierre.
- La integración emitió una factura L119.00, aplicó L100.00 en efectivo y dejó L19.00; luego envió dos transferencias concurrentes por L19.00 y obtuvo exactamente un `201` y un `409`. El documento terminó con dos aplicaciones, total cobrado L119.00, saldo cero y medio derivado `Mixto`.
- Repetir el pago en efectivo con la misma clave/cuerpo devolvió el mismo ID; reutilizar la clave con L99.00 devolvió `409`. Caja cerrada, clave ausente y sobrecobro también fueron rechazados. Persistieron dos pagos, dos aplicaciones, dos asientos, dos auditorías y dos intenciones válidas.

### Devengo, caja y notas de crédito

- La factura registra Debe `1103 Cuentas por cobrar` y Haber ingreso/impuestos. Cada cobro genera un asiento separado: efectivo debita `1101 Caja`, transferencia `1102 Banco` y tarjeta `1104 Cobros con tarjeta por liquidar`; todos acreditan `1103` por el importe aplicado.
- Solo el pago efectivo crea `CashMovement`, referenciado por `Payment.Id`. En el caso automatizado, apertura L25.00 + cobro L100.00 produjo saldo L125.00; la transferencia no creó movimiento. El cambio L100.00 sobre L200.00 recibido se calculó en servidor y no aumentó la caja por encima del importe aplicado.
- Se corrigió la nota de crédito: primero acredita la cuenta por cobrar pendiente y solo lleva a `2105 Saldos a favor de clientes` la porción que ya había sido cobrada. El crédito total de la factura pendiente usada por integración redujo `1103` por L119.00 y no creó un pasivo ficticio.
- El checkout total crea factura, `Payment`, aplicación, movimiento si corresponde, asiento de devengo y asiento de cobro en la misma transacción. Su prueba actualizada espera dos asientos y vincula caja al pago, no a la factura.

### Migración e inmutabilidad fiscal

- La migración `20260909144804_AddPaymentLedger` crea las dos tablas, FKs restrictivas, unicidad del número, unicidad pago–factura, índices por fecha/factura y checks de importe, HNL, estado y coherencia de campos por método. Se ensayó `up → down` hasta `HardenCashRegisterClosure` y `up` nuevamente antes de cargar pagos sintéticos.
- No se fabrican pagos para documentos históricos a partir de `Status` o `PaymentMethod`: esos campos no demuestran importe, fecha, caja ni referencia reales. La conciliación/importación de historia continúa abierta en S29 y debe resolverse sobre copia antes del corte real.
- Los cobros posteriores no modifican `Invoice.PaymentMethod`, `CashReceived`, `CashChange`, `FiscalSnapshotJson` ni `FiscalHash`. El DTO deriva el resumen de medios desde aplicaciones. Una prueba conserva snapshot/hash byte por byte después de los pagos.
- ESC/POS dejó de convertir `null` en efectivo. Una factura emitida sin cobro muestra `Condición de pago: Pendiente de cobro`; si luego recibe un abono, la reimpresión mantiene esa condición original mientras el auxiliar muestra el movimiento actual.

### Interfaz y evidencia observada

- Facturación presenta columnas de total y saldo, y al seleccionar un documento muestra total, cobrado, notas de crédito y saldo. El auxiliar lista número de pago, método, fecha/hora en `America/Tegucigalpa`, referencia e importe.
- El diálogo de abono distingue efectivo, tarjeta y transferencia; limita el importe al saldo, ofrece solo cajas activas y abiertas, calcula cambio en vivo y exige referencia para medios no efectivos. Tiene labels asociados, rol de diálogo, cierre accesible, estado de envío y bloqueo de doble clic.
- Recorrido visual sobre PostgreSQL: factura L119.00, caja abierta L50.00, abono en efectivo L100.00 con L120.00 recibido y cambio L20.00. La tabla y el resumen cambiaron a cobrado L100.00/saldo L19.00 y apareció un único `PAG-…` en el auxiliar.
- Segundo recorrido: factura pendiente L57.50, transferencia L10.00, DTO con cobrado L10.00/saldo L47.50 y reimpresión todavía rotulada `Pendiente de cobro`. Esto comprobó que cobro y representación fiscal tienen fuentes separadas.

### QA y límites

- `scripts/qa.sh` terminó en código 0: build .NET con 0 advertencias/errores; 44 unitarias y 1 integración PostgreSQL aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite con 2.095 módulos y `git diff --check` aprobado.
- `dotnet ef migrations has-pending-model-changes --no-build` informó que no hay cambios pendientes y quedaron 0 bases `hotel_erp_it_%`.
- Este lote cubre el auxiliar de cobros directos y el pago inmediato del checkout. Siguen abiertos `Refund`, anticipos, aplicación de saldo a favor, liquidación/adquirencia de tarjeta, conciliación bancaria, migración histórica y aprobación contable/operativa externa; por ello G2 y G3 continúan abiertas.

## 2026-09-09 — lote 9: reembolsos y secuencia de caja

### Auxiliar, elegibilidad y concurrencia

- Se añadieron `Refund` y `RefundApplication` con número propio, pago original, usuario, fecha, estado, moneda HNL, medio, referencia, motivo, caja, asiento y aplicaciones a notas de crédito. `Payment` deriva `RefundedAmount` y pasa a `ParcialmenteReembolsado` o `Reembolsado` sin alterar la factura fiscal.
- `POST /api/payments/{paymentId}/refunds` exige permiso de caja e `Idempotency-Key` UUID. La suma de aplicaciones debe coincidir exactamente con el importe; cada nota debe pertenecer a una factura pagada por el cobro indicado y conservar crédito no reembolsado suficiente.
- El servidor limita además el total al saldo real a favor del cliente: cobros confirmados + créditos confirmados − total de facturas − reembolsos previos. El navegador no puede crear efectivo a partir de una nota pendiente ni devolver más que el pago, la nota o el pasivo disponibles.
- La operación bloquea primero el pago y luego las facturas por ID ordenado; efectivo toma también el lock común de caja. Dos solicitudes simultáneas sobre el mismo saldo produjeron exactamente un `201` y un `409`; repetir la ganadora con la misma clave/cuerpo devolvió el mismo ID y reutilizar la clave con otro importe devolvió `409`.
- El medio del reembolso debe ser el mismo del pago original. Efectivo exige caja abierta y saldo suficiente; transferencia y tarjeta exigen referencia externa. El motivo normalizado es obligatorio y queda en el auxiliar y la auditoría.

### Contabilidad y efectivo

- La nota de crédito de una venta cobrada reconoce el pasivo `2105 Saldos a favor de clientes`. El reembolso lo cancela con Debe `2105` y Haber `1101 Caja`, `1102 Banco` o `1104 Cobros con tarjeta por liquidar`, según el medio original; el asiento se identifica de forma única por el origen `Refund`.
- En el escenario de efectivo, una factura y pago por L 119.00, seguida de crédito total y reembolso L 119.00, dejó el pasivo en cero. La caja pasó de apertura L 30.00 a L 149.00 con el cobro y volvió a L 30.00 con el egreso; el movimiento referencia el reembolso y su motivo.
- Un escenario separado por transferencia devolvió L 119.00 sin crear movimiento de efectivo y comprobó Debe `2105` / Haber `1102`. Intentar la devolución sin referencia externa fue rechazado antes de guardar filas.
- `GET /api/payments/{paymentId}/refunds` y el detalle del pago presentan el mismo auxiliar persistido, sus aplicaciones, usuario, caja, asiento y referencias. No se fabrican devoluciones históricas a partir del estado de una factura o pago.

### Defectos encontrados durante la verificación

- Apertura y cierre de caja dependían del valor UTC de `CreatedAt`, mientras los demás movimientos asignaban hora de Honduras. Ordenar por `MovementDate` podía colocar la apertura seis horas después de un cobro y hacer que el siguiente movimiento partiera de un saldo antiguo. Apertura/cierre ahora asignan `HondurasTime.Now`; todas las lecturas del último movimiento ordenan por `CreatedAt` e `Id` y usan un índice concordante.
- `AddRefundLedger` normaliza aperturas/cierres históricos únicamente cuando la diferencia entre `CreatedAt` y `MovementDate` identifica el desfase UTC de seis horas. La condición es idempotente; el descenso no vuelve a sumar horas porque eso corrompería movimientos locales creados después de instalarla.
- Crear una autorización documental podía persistirla y responder 500 porque la entidad guardaba `DateOnly` y el DTO declaraba `DateTime`. El contrato quedó alineado en `DateOnly`; la integración crea por HTTP las autorizaciones de crédito y débito y comprueba su fecha antes de usarlas.
- La consulta del pago durante el reembolso cargaba dos colecciones en una sola sentencia. Se añadió `AsSplitQuery` para evitar el producto cartesiano y la advertencia de EF sin cambiar los locks ni la transacción.

### Interfaz y revisión visual

- La pantalla de facturación calcula crédito elegible, consulta solo notas relacionadas mediante `originalInvoiceId` y limita importe y aplicaciones. El diálogo explica el pago de origen, exige motivo, ofrece únicamente cajas abiertas para efectivo y solicita referencia para medios externos.
- La intención UUID se conserva en `sessionStorage` ante una respuesta incierta, los controles se bloquean durante el envío y el auxiliar muestra estado, fecha de Honduras, importe, medio, referencia, motivo y notas aplicadas.
- Recorrido visual sobre PostgreSQL: factura `001-001-01-00000001` por L 119.00, pago completo, nota de crédito `003-001-03-00000001` y devolución completa. La interfaz mostró confirmación `Reembolso REF-… registrado por L119`, el pago pasó a `Reembolsado` y el auxiliar presentó una sola devolución. La consulta SQL confirmó caja L 100.00 después de apertura L 100.00 + cobro L 119.00 − egreso L 119.00.

### Migración, QA y límites

- Migración agregada: `20260909200231_AddRefundLedger`. Crea tablas, FKs restrictivas, checks, unicidades e índices del auxiliar y sustituye el índice cronológico de caja por `(CashRegisterId, CreatedAt, Id)`.
- En una base PostgreSQL vacía se ejecutó instalación hasta la última migración, descenso a `20260909144804_AddPaymentLedger`, ascenso y `dotnet ef migrations has-pending-model-changes --no-build`; el modelo quedó sin cambios pendientes y la base temporal fue eliminada.
- `scripts/qa.sh` terminó en código 0: backend con 0 advertencias/errores; 44 pruebas unitarias y 1 integración PostgreSQL aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite con 2.095 módulos y `git diff --check` aprobado. No quedaron bases `hotel_erp_test_%`, `hotel_erp_it_%` ni la base de migración.
- El auxiliar técnico queda implementado y probado. Continúan abiertas la anulación/reversión de un reembolso confirmado, la aprobación segregada definida por monto/medio, anticipos, aplicación de saldo a favor a ventas futuras, adquirencia de tarjeta, conciliación bancaria y las aprobaciones contable, operativa y SAR aplicables.

## 2026-09-09 — lote 10: liquidación de tarjeta

### Lote del adquirente y concurrencia

- Se añadieron `CardSettlement` y `CardSettlementApplication` con número propio, usuario, fecha de depósito, estado, moneda HNL, importe bruto, depósito bancario, comisión, retención, referencia externa, asiento y pagos aplicados.
- `POST /api/card-settlements` exige permiso contable e `Idempotency-Key` UUID. La suma de aplicaciones debe coincidir exactamente con el bruto y la igualdad `depósito + comisión + retención = bruto` se valida después de redondear a centavos.
- Solo admite pagos confirmados con tarjeta y limita cada aplicación al pago menos reembolsos y liquidaciones confirmadas. La referencia normalizada es única; no se puede registrar una fecha futura ni aplicar dos veces el mismo pago dentro del lote.
- Liquidaciones y reembolsos usan el mismo advisory lock `payment-financial:{id}`. Dos lotes concurrentes sobre un pago L 119.00 produjeron exactamente un `201` y un `409`; el reintento de la solicitud ganadora devolvió el mismo ID y una variante con la misma clave devolvió `409`.
- Un pago con tarjeta ya aplicado a una liquidación no admite el reembolso simple que acreditaría de nuevo la cuenta transitoria. La API conserva el saldo a favor y exige el futuro flujo de reversión/contracargo del adquirente, evitando crear un mayor incoherente.

### Contabilidad y trazabilidad

- El cobro original mantiene Debe `1104 Cobros con tarjeta por liquidar` / Haber `1103 Cuentas por cobrar`. La liquidación registra Debe `1102 Banco`, `5201 Comisiones por adquirencia` y `1105 Retenciones sufridas por acreditar`; Haber `1104` por el bruto.
- El caso sintético PAG07 quedó en L 119.00 = depósito L 114.00 + comisión L 3.00 + retención L 2.00. PostgreSQL confirmó cuatro líneas balanceadas y cero `CashMovement` con referencia a la liquidación; el ingreso fiscal no se volvió a contabilizar ni se redujo por el depósito neto.
- `GET /api/card-settlements/eligible-payments` proyecta en SQL únicamente los datos del pago y las sumas correlacionadas de reembolso/liquidación; no materializa las colecciones históricas completas. Tras confirmar el lote, el pago desaparece de elegibles y su DTO informa `CardSettledAmount = 119.00`.
- El alta conserva un evento `CreateCardSettlement`, un registro idempotente y un asiento identificado de forma única por `ReferenceId`. Los listados devuelven pagos, referencias POS y responsable para recorrer auxiliar ↔ cobro ↔ asiento.

### Interfaz y revisión visual

- Se creó el módulo “Liquidaciones de tarjeta” para Admin/Contador, enlazado desde sidebar, buscador y breadcrumbs. Presenta total pendiente, selección del lote, depósito calculado y el historial con sus cuatro componentes.
- La pantalla selecciona únicamente cobros elegibles entregados por la API; comisión y retención no pueden exceder el bruto, la fecha no puede ser futura y el depósito es de solo lectura. Conserva la intención UUID ante una respuesta incierta y deshabilita todos los controles durante el envío.
- La primera inspección mostró que el UUID completo del pago forzaba scroll horizontal y comprimía fecha/importe. La presentación ahora conserva prefijo y sufijo, deja el valor completo como descripción y mostró todas las columnas en un viewport de escritorio.
- Recorrido sobre PostgreSQL de QA: el pago `PAG-01A087FBE8097017AC567AA0141F0579`, referencia POS `POS-QA-0009821`, se liquidó con referencia `BAC-QA-LOTE-009821`. La interfaz calculó depósito L 114.00, mostró el mensaje `LTJ-… registrada` y cambió a “No hay cobros pendientes”; el historial reflejó L119/L3/L2/L114.

### Migración, QA y límites

- Migración agregada: `20260909205332_AddCardSettlementLedger`. Crea tablas, relaciones restrictivas, precisiones, checks de componentes/estado/moneda, referencias y números únicos e índices por pago y fecha.
- En una base PostgreSQL vacía se ejecutó instalación completa, descenso a `20260909200231_AddRefundLedger`, ascenso y `migrations has-pending-model-changes --no-build`; no hubo cambios pendientes y la base temporal se eliminó.
- `scripts/qa.sh` terminó en código 0: build con 0 advertencias/errores; 44 unitarias y 1 integración PostgreSQL aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite con 2.096 módulos y `git diff --check` aprobado. La optimización final de la consulta elegible repitió build e integración completos con 0 fallos.
- El mapeo de cuentas `1102/1104/1105/5201` implementa el oráculo sintético del plan, pero sus códigos y la deducibilidad/clasificación fiscal de comisiones y retenciones requieren aprobación DEC-09 del contador. Permanecen abiertos importación/conciliación de estado bancario, reversos o contracargos, evidencia del adquirente, migración histórica y aprobación externa.

## 2026-09-09 — lote 11: límites de autenticación y adjuntos fiscales

### Resistencia a abuso y errores seguros

- ASP.NET aplica ventanas fijas independientes a `login`, `refresh` y `change-password`. La partición usa la IP observada para solicitudes anónimas y el ID estable del usuario después de autenticarse; no confía en cabeceras reenviadas enviadas por el cliente.
- Los valores operativos están tipados y se validan al arrancar. La configuración distribuida limita por defecto login a 10 solicitudes/minuto, refresh a 30/minuto y cambio de contraseña a 5/5 minutos, sin cola. Un rechazo responde `429 application/problem+json`, `Retry-After`, segundos de espera y `traceId`.
- Login ya no distingue por HTTP entre usuario inexistente, cuenta inactiva o bloqueo persistente. Todas esas condiciones devuelven `401` y el mismo detalle. Al completar un login posterior a un bloqueo vencido se limpia también `LockoutEnd`.
- `AddProblemDetails` y el manejador global convierten excepciones no controladas en una respuesta 500 trazable. Una prueba sustituyó el servicio de autenticación por una excepción sintética que contenía términos SQL y stack; el cuerpo HTTP no devolvió ese mensaje, proveedor ni pila.
- La integración configuró una ventana de 5 segundos y 2 permisos: los dos primeros intentos inválidos devolvieron `401`, el tercero `429`, y un acceso válido fue admitido después del vencimiento. Las pruebas de reflexión fijan las tres políticas para evitar que un cambio de ruta retire el control.

### Almacén privado de autorizaciones SAR

- Los PDF nuevos se guardan con la clave `{DocumentAuthorization.Id:N}.pdf` dentro de un directorio configurable. Nombre original y CAI no forman parte de la ruta; el DTO solo informa `hasAttachment` y ya no expone una ruta absoluta del equipo Windows.
- El servidor exige archivo no vacío, máximo configurable de 10 MB, extensión `.pdf`, MIME permitido, cabecera `%PDF-` y marcador `%%EOF` en el tramo final. Copia primero a un nombre temporal impredecible, vuelve a medir mientras transmite, valida antes de mover y elimina el archivo si falla la persistencia en PostgreSQL.
- La descarga vuelve a resolver la ruta canónica dentro del almacén, rechaza enlaces simbólicos y contenido que perdió su envoltura PDF, exige `ManageTaxes`, y envía `application/pdf`, `Content-Disposition` por ID, `Cache-Control: no-store` y `X-Content-Type-Options: nosniff`.
- La interfaz usa la fecha civil que requiere `DateOnly`, muestra el máximo de 10 MB, filtra PDF y obtiene el blob mediante el cliente Axios autenticado. Abre una vista segura cuando el navegador lo permite y descarga como alternativa, sin depender de una URL sin bearer.
- SEC09 automatizada rechazó texto renombrado a PDF, MIME `text/plain`, 10 MB + 1 byte y una referencia persistida `../outside.pdf`. Un archivo cuyo nombre enviado era `../../…` quedó bajo la clave del ID, se descargó byte por byte para Admin y devolvió `403` a un usuario Recepción.

### QA y límites

- `scripts/qa.sh` con PostgreSQL real terminó en código 0: build con 0 advertencias/errores; 48 unitarias y 2 integraciones aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite con 2.096 módulos y validación de conflictos aprobada.
- La prueba eliminó su base y directorio temporal; quedaron 0 bases `hotel_erp_it_%`, `hotel_erp_test_%` o `hotel_erp_migration_qa` y 0 carpetas `/tmp/hotel-erp-it-attachments-*`.
- El rate limit reside en la única instancia local prevista y se reinicia junto con la API; el bloqueo por cinco contraseñas incorrectas de una cuenta conocida continúa persistido en PostgreSQL durante 15 minutos. Si luego se coloca un proxy delante de la API, deberá configurarse la confianza de IP antes de usar cabeceras reenviadas.
- La envoltura y el tipo reducen archivos falsos, pero no sustituyen un antivirus, desarme de contenido o validación criptográfica del documento SAR. Los adjuntos todavía deben incorporarse al respaldo/restauración y a la tabla de conservación DEC-12 antes de liberar el sistema.
## 2026-09-09 — lote 12: matriz de autorización

### Roles y privilegio mínimo

- El catálogo inicial incorpora cuatro perfiles con identidad estable: `Admin` (`administrator`), `Recepcion` (`reception`), `Caja` (`cashier`) y `Contador` (`accountant`). Caja recibe únicamente `manage_cash`; Contador recibe fiscal, contabilidad, reportes, exportación y auditoría; Recepción conserva emisión, reservaciones y caja.
- Se agregó `manage_inventory`. Inventario queda reservado a Admin, mientras compras y proveedores exigen `manage_accounting`. Crear o modificar habitaciones, tipos de habitación, clientes y huéspedes exige `manage_reservations`; cambiar el catálogo de descuentos y operar las pruebas de impresora exige `manage_settings`.
- La matriz completa de familias, lecturas, mutaciones y justificaciones se conserva en [matriz-autorizacion.md](../matriz-autorizacion.md). Las lecturas compartidas están limitadas a recursos necesarios para facturar, cobrar o revisar; los adjuntos PDF fiscales siguen requiriendo `manage_taxes` incluso para descarga.

### Cobertura automática de endpoints

- Una prueba por reflexión inventaría todos los controladores y falla si una acción HTTP no declara autenticación o acceso público explícito. Una segunda prueba revisa todos los `POST`, `PUT`, `PATCH` y `DELETE` y exige una política con nombre, salvo logout, cambio de la propia contraseña y reimpresión de un comprobante persistido.
- La integración PostgreSQL recorre anónimamente más de 60 acciones reales con rutas y tipos de contenido válidos; todas las privadas devolvieron `401`. `login`, `refresh` y `/health` son los únicos accesos públicos justificados.
- En una base efímera se crearon usuarios Admin, Recepcion, Caja y Contador. La prueba comparó sus permisos exactos y confirmó accesos permitidos/prohibidos sobre caja, contabilidad, adjuntos, usuarios, respaldos, auditoría, inventario, proveedores, habitaciones y descuentos. Los recursos protegidos devolvieron `403` antes de ejecutar el caso de uso; los autorizados llegaron a `200`, `404` o validación `400` según el dato sintético.

### Interfaz y contratos

- Rutas, sidebar y buscador usan los nombres sembrados `Admin`, `Recepcion`, `Caja` y `Contador`. Las páginas completas de reservaciones, caja, fiscal, contabilidad, reportes, auditoría, respaldo e inventario validan el mismo permiso que el backend.
- Contador puede consultar clientes y huéspedes, pero la interfaz oculta alta, edición y eliminación porque no posee `manage_reservations`. En facturación, notas de crédito/débito requieren `create_invoices`; cobros y devoluciones requieren `manage_cash`.
- El formulario administrativo de usuarios ya ofrece los roles Caja/Contador sembrados y muestra el mínimo real de 12 caracteres para contraseñas iniciales y restablecidas, en concordancia con el contrato del servidor.

### QA y alcance pendiente

- `scripts/qa.sh` terminó con código 0: backend con 0 advertencias y 0 errores; 76 pruebas unitarias y 3 integraciones PostgreSQL aprobadas; instalación reproducible con `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite compiló 2.096 módulos; la validación de espacios y conflictos fue aprobada.
- No quedaron bases `hotel_erp_it_%`/`hotel_erp_test_%`, directorios temporales de adjuntos ni archivos en el almacén predeterminado. El alta de permisos y perfiles no necesita migración de esquema: el sembrador idempotente completa el catálogo al arrancar.
- El sembrador no retira permisos añadidos manualmente a roles de una instalación existente. Antes de migrar una base real se debe exportar y revisar sus concesiones; también continúan pendientes la política LAN de cookie/CSRF/CORS, la rotación operativa de secretos, SEC11 con una cuenta PostgreSQL restringida y la revisión técnica independiente.

## 2026-09-10 — lote 13: sesión de navegador, CSRF y CORS

### Custodia y renovación de sesión

- El access token pasó de 60 a 15 minutos y se mantiene únicamente en memoria. El almacén Zustand inicia vacío y elimina `accessToken`, `refreshToken` y `user` que una versión anterior pudiera haber dejado en `localStorage`.
- Login y refresh ya no serializan el token de renovación. El servidor lo emite y rota mediante `hotel_erp_refresh`, restringida a `/api/auth`, `HttpOnly`, `SameSite=Strict` y `Secure` fuera de Development. Una cookie separada `hotel_erp_csrf`, legible y aleatoria, debe coincidir con `X-CSRF-Token` para renovar por cookie.
- El frontend intenta una única renovación al arrancar, incluso bajo el montaje doble de React StrictMode. Conserva el acceso en memoria, comparte una renovación entre solicitudes concurrentes y usa Web Locks para serializar rotaciones entre pestañas del mismo origen cuando la API está disponible. Limpia estado/cookies mediante el logout del servidor.
- El alta administrativa crea el usuario con cambio obligatorio de contraseña y ya no devuelve una sesión que suplante al usuario creado. La auditoría atribuye el alta al administrador autenticado.

### SEC07, SEC08 y política LAN

- La integración confirmó `Cache-Control: no-store`, ausencia de refresh en JSON, atributos `HttpOnly`/`SameSite=Strict`, cookie CSRF no `HttpOnly`, rechazo `403` sin cabecera y rotación correcta con la cabecera coincidente.
- Un preflight desde `http://localhost` recibió el origen exacto y `Access-Control-Allow-Credentials: true`; `https://evil.example` no recibió autorización. El arranque valida que la lista CORS sea explícita y rechaza comodines, rutas, consultas, fragmentos y credenciales embebidas.
- Los orígenes HTTP de Vite/localhost se movieron a `appsettings.Development.json`; Production distribuye una lista vacía y exige que operación declare la URL HTTPS real. Así, asignar el índice cero por variable de entorno no conserva otros orígenes locales heredados.
- Reutilizar el refresh anterior después de rotarlo devolvió `401` y revocó su familia. Logout invalidó el access y el refresh en servidor; esta cobertura se conserva junto con revocación inmediata al cambiar contraseña o versión de seguridad.
- Recorrido real en el navegador local: login de administrador, carga del panel y una segunda carga independiente desde `/` que ejecutó `POST /api/auth/refresh`, respondió `200` y abrió `/dashboard`. Después del logout, otra carga desde `/` ejecutó el mismo bootstrap, recibió `401` y permaneció en `/login`. También se comprobó el cierre desde el menú móvil.
- La configuración y el procedimiento de instalación quedan definidos en [sesion-y-transporte-lan.md](../sesion-y-transporte-lan.md): mismo origen HTTPS, certificado confiable, orígenes exactos y puerto limitado a la red de personal.

### QA y alcance pendiente

- `scripts/qa.sh` con PostgreSQL real terminó con código 0: backend con 0 advertencias y 0 errores; 76 pruebas unitarias y 3 integraciones aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite compiló 2.098 módulos; la validación de espacios y conflictos fue aprobada.
- Las pruebas temporales eliminaron sus bases y almacenes de adjuntos. El cambio no requiere migración de esquema.
- SEC06–SEC10 tienen evidencia técnica automatizada. Su aprobación organizativa permanece abierta hasta que un revisor independiente firme el resultado y operación pruebe en Windows 11 el certificado, DNS, firewall y revocación entre dos equipos/pestañas. También permanecen pendientes la rotación operativa de secretos y SEC11 con una cuenta PostgreSQL restringida.

## 2026-09-10 — lote 14: cuenta PostgreSQL de mínimo privilegio

### Migración y ejecución separadas

- Production establece `Database:ApplyMigrationsOnStartup=false` y rechaza habilitarlo fuera de Development. La API compara el modelo con `__EFMigrationsHistory` y aborta con una instrucción controlada si existe una migración pendiente; Development conserva migración automática para el ciclo local y las bases efímeras de integración.
- Se añadió una fábrica de diseño que solo acepta `HOTEL_ERP_MIGRATION_CONNECTION`. El manifiesto local fija `dotnet-ef` 10.0.8, por lo que una actualización de esquema no requiere entregar a la API diaria la credencial dueña.
- El instalador PowerShell y sus dos scripts `psql` crean `hotel_erp_migrator` y `hotel_erp_app` sin superusuario, creación de roles/bases, herencia, replicación o bypass RLS. La base/esquema pertenecen al migrador; la cuenta diaria recibe conexión, uso y DML sobre las tablas operativas.
- Los privilegios públicos de base/esquema se retiran. `AuditLogs` queda en `SELECT/INSERT` y `__EFMigrationsHistory` en `SELECT`; la aplicación no recibe `CREATE` ni `TEMP`. Los privilegios predeterminados permiten que una migración posterior conceda DML sobre sus nuevas tablas y secuencias.

### SEC11 y prueba del instalador

- Una cuarta integración PostgreSQL migró y sembró primero con el propietario, arrancó después la API con el rol restringido y completó health/login, incluida la inserción de su evento de auditoría.
- Bajo la conexión diaria, `CREATE TABLE`, inserción falsa en `__EFMigrationsHistory`, `UPDATE`/`DELETE` de `AuditLogs` y lectura de `pg_authid` fueron rechazados con SQLSTATE `42501`. También se verificaron en falso `rolsuper`, `rolcreatedb`, `rolcreaterole`, `rolinherit`, `rolreplication`, `rolbypassrls`, `CREATE` y `TEMP`.
- Los scripts SQL se ejecutaron aparte sobre otra base limpia: crearon ambos roles, aplicaron las 11 migraciones como migrador, concedieron acceso a la aplicación, permitieron leer 11 filas de historial y rechazaron DDL. La base y los roles sintéticos fueron eliminados al terminar.
- `dotnet-ef migrations has-pending-model-changes --no-build` confirmó que el modelo actual está representado por las migraciones existentes.

### QA y alcance pendiente

- `scripts/qa.sh` terminó con código 0: backend con 0 advertencias y 0 errores; 76 pruebas unitarias y 4 integraciones PostgreSQL aprobadas; `npm ci`; `npm audit` con 0 vulnerabilidades; ESLint limpio; TypeScript/Vite compiló 2.098 módulos y la validación de espacios/conflictos fue aprobada.
- No quedaron bases `hotel_erp_it_%`/`hotel_erp_provision_it_%`, roles `hotel_erp_*_it_%`, procesos de API ni directorios temporales de adjuntos.
- SEC11 queda aprobado técnicamente. Antes de la instalación real faltan inventario/respaldo de la base oficial, custodia de ambas contraseñas, ejecución del instalador en Windows 11 y firma del revisor independiente. La cuenta de respaldo y el procedimiento de rotación pertenecen al trabajo operativo restante de G1/G6.
