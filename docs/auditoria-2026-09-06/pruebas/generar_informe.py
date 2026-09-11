"""Genera los entregables de auditoría; nunca modifica código del producto."""
from pathlib import Path
import json, hashlib, collections, re, csv, statistics
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent.parent
B='backend/src/hotel-erp.Api/'
F='frontend/src/'
records=[]
def add(id,p,area,title,refs,evidence,impact,fix,test):
    locations=[]
    for path,needle in refs:
        text=(ROOT/path).read_text(errors='replace')
        line=next((n for n,s in enumerate(text.splitlines(),1) if needle in s),None)
        if line is None: raise ValueError((id,path,needle))
        locations.append({'archivo':path,'linea':line,'ancla':needle})
    records.append(dict(id=id,prioridad=p,area=area,titulo=title,ubicaciones=locations,evidencia=evidence,impacto=impact,recomendacion=fix,aceptacion=test))
def b(s,n):return (B+s,n)
def f(s,n):return (F+s,n)

add('FIN-01','P0','contabilidad','Las notas de crédito aumentan ingresos en lugar de revertirlos',
 [b('Services/AccountingService.cs','CreateInvoiceEntryAsync'),b('Controllers/InvoicesController.cs','CreateCreditNote')],
 'Reproducido T09: factura base L100 y nota base L100 producen dos créditos positivos a ingresos. El ISV de la nota fue cero por FIN-03; aun así el error de signo del asiento queda demostrado. La original se marca Anulada sin reversar su asiento.',
 'El saldo de ingresos llega a L200 cuando la devolución total debería neutralizar L100. Una nota parcial también anula la factura completa; no hay control acumulado del importe ya acreditado.',
 'Modelar factura, nota de crédito y débito como eventos distintos; invertir el asiento de la nota de crédito, enlazar líneas de origen y limitar el acumulado acreditado. Mantener el documento original y su historia.',
 'Factura 100+15; nota total 100+15: ingreso e ISV netos cero, devolución/cuenta por pagar conciliada. Dos notas parciales nunca exceden el saldo disponible.')
add('FIN-02','P0','contabilidad','Las operaciones se guardan parcialmente antes de devolver un error',
 [b('Controllers/ReservationsController.cs','CheckIn('),b('Controllers/InvoicesController.cs','Create('),b('Database/Repositories/InvoiceRepository.cs','public async Task AddAsync(Invoice')],
 'T03: check-in sin CAI devuelve 400, pero deja reserva CheckIn, habitación ocupada y folio. T32–T33: factura de L0.04 queda persistida sin asiento tras un 500. Correlativo, factura, asientos y bitácora se guardan en pasos separados.',
 'Reintentar después del error puede duplicar documentos, consumir correlativos o dejar estadías y contabilidad inconsistentes. Una transacción interna de SaveChanges no abarca el caso de uso completo.',
 'Una transacción de PostgreSQL por operación de negocio, con validaciones previas y clave de idempotencia. Correlativo, documento, folio, pago, asiento y evento de auditoría deben confirmar juntos; impresión y nube van después mediante cola persistente.',
 'Inyectar fallo en cada paso y comprobar rollback completo. Repetir la misma clave devuelve el mismo resultado y no crea otra venta.')
add('FIN-03','P1','contabilidad','Las notas calculan impuestos desde LineTotal suministrado por el cliente',
 [b('Controllers/InvoicesController.cs','var isvAmount = request.Items.Sum'),f('pages/invoices/InvoicesPage.tsx','handleCreditNoteSubmit')],
 'T09: cantidad 1, precio 100 y tasa 15%, sin LineTotal en JSON, generan subtotal100 e ISV0. La interfaz construye líneas sin ese campo. El subtotal se recalcula por un camino y el impuesto por otro.',
 'El importe de impuestos depende de un campo manipulable o ausente; también afecta notas de débito.',
 'Calcular base, descuentos e impuestos exclusivamente en servidor a partir de cantidades y precios autorizados; eliminar totales derivados de los DTO de entrada.',
 'Omitir, falsear o enviar LineTotal=999 no cambia el resultado correcto de la nota.')
add('FIN-04','P1','contabilidad','Desglose fiscal de tasas y bases incorrecto',
 [b('Controllers/InvoicesController.cs','ISV15Amount ='),b('Controllers/PurchaseInvoicesController.cs','ISV15Amount ='),b('Controllers/ReportsController.cs','salesTaxed15')],
 'T13: base100 a18% guarda ISV15Amount=18 e ISV18Amount=0. T14: línea exenta100 guarda TaxableAmount100 y ExemptAmount0. El resumen de base15 suma todo el subtotal de documentos con ISV15 positivo, incluso documentos mixtos.',
 'Los libros y resúmenes no representan las bases gravadas, exentas y exoneradas reales. No se está dictaminando la tasa legal aplicable a cada producto: falla incluso la clasificación de la tasa solicitada.',
 'Motor fiscal único por línea, con base y tasa explícitas, redondeo definido y agregados separados. Usarlo en ventas, compras, notas, hospedaje y reportes.',
 'Documento mixto con bases exenta100, 15%100 y18%100: cada casilla debe coincidir con sus líneas; validar exoneración global y parcial.')
add('FIN-05','P1','contabilidad','Redondeo monetario inconsistente rompe la partida doble',
 [b('Controllers/InvoicesController.cs','TotalAmount = Math.Round'),b('Services/AccountingService.cs','if (debit != credit)')],
 'T32: base0.03 con15% y4%: componentes redondeados0.03+0+0 frente a total0.04. AccountingService rechaza el asiento después de guardar la factura. T15 fue un control negativo: su primer ejemplo sí pasó, no constituye reproducción del defecto.',
 'Importes legítimos de centavos pueden fallar y dejar documentos huérfanos; otras rutas redondean de forma distinta.',
 'Definir una política monetaria única y hacer que el total sea la suma de componentes contabilizados, con ajuste explícito de redondeo cuando corresponda. Validar escala y límites en DTO y BD.',
 'Casos de medio centavo, varias líneas, descuento y exoneración deben mantener suma de componentes=total y débito=crédito a la precisión monetaria.')
add('FIN-06','P1','contabilidad','Notas fiscales editables dejan hash y asientos obsoletos',
 [b('Controllers/InvoicesController.cs','Update(')],
 'T11: una nota emitida se edita de100 a25; FiscalHash permanece igual y su asiento sigue por100. El bloqueo de edición solo considera Factura. T16 acepta creación directa de NotaCredito sin documento original ni motivo.',
 'Documento, snapshot y mayor contable dejan de describir la misma operación. Se puede eludir el flujo específico de notas.',
 'Inmutabilidad de todos los documentos emitidos; corregir mediante documento compensatorio y autorización apropiada. DTO y rutas por tipo con origen obligatorio y enum validado.',
 'PUT de cualquier documento emitido es rechazado; no existen notas huérfanas ni tipos numéricos fuera del enum.')
add('FIN-07','P1','contabilidad','Los libros suman documentos sin aplicar su naturaleza ni estado',
 [b('Controllers/ReportsController.cs','GetTaxSummary'),b('Controllers/ReportsController.cs','sales-book')],
 'Código y T10: agregados sin filtro de tipo/estado ni signo para notas; una factura anulada permanece en la selección y su nota entra con signo positivo. El ejemplo T10 no demuestra duplicación del ISV porque FIN-03 dejó el impuesto de la nota en cero.',
 'Ingresos e impuestos pueden incluir documentos anulados, notas y otros tipos como ventas normales. La UI llama a estas salidas oficiales sin una conciliación demostrada.',
 'Definir libro de eventos fiscales por tipo y signo, con trazabilidad al original; conciliar contra mayor y documentos, y validar formato con el responsable contable antes de usarlo para declarar.',
 'Mes con factura, nota parcial, nota total y documento no fiscal: libro, resumen y mayor coinciden y no incluyen ventas inexistentes.')
add('FIN-08','P1','contabilidad','Pagar compras no registra la salida de dinero',
 [b('Controllers/PurchaseInvoicesController.cs','MarkAsPaid'),b('Services/AccountingService.cs','CreatePurchaseEntryAsync')],
 'T18: PUT /purchase-invoices/{id}/pay cambia estado; permanece un único asiento de compra. Toda compra usa gasto5109 y crédito a proveedores, sin modelo de pago, vencimiento o clasificación de activo/inventario.',
 'Una factura pagada sigue como deuda contable y no reduce caja/banco. Compras de existencias o activos terminan en gastos genéricos.',
 'Registrar pagos aplicados a documentos, con asiento, caja/banco y referencia. Permitir clasificación contable de líneas y tratar impuestos recuperables según política validada.',
 'Compra115 a crédito y pago115 dejan proveedor0 y banco -115; probar abonos, reverso y compra de activo/inventario.')
add('FIN-09','P1','contabilidad','Compras duplicadas y borrado con saldos contradictorios',
 [b('Controllers/PurchaseInvoicesController.cs','Create('),b('Services/AccountingService.cs','DeleteEntryByReferenceIdAsync'),b('Database/Repositories/AccountingRepository.cs','GetAllAccountBalancesAsync')],
 'T19 registra dos compras del mismo proveedor con igual número. T20 borra una y el catálogo muestra proveedor230 mientras el balance de comprobación muestra115: se marca eliminado el asiento por una ruta que no elimina sus líneas y los agregados aplican filtros distintos.',
 'Duplica gastos/deudas y ofrece saldos diferentes para una misma cuenta.',
 'Clave de documento de proveedor definida con el contador e índice único; reemplazar borrado de documentos contabilizados por reversos. Unificar filtros de cabecera/líneas en todos los agregados.',
 'Duplicado concurrente rechazado; todos los informes concilian antes y después de un reverso.')
add('FIN-10','P1','contabilidad','Caja y arqueos no representan cobros reales',
 [b('Controllers/CashRegistersController.cs','Close('),f('pages/reservations/CheckOutPage.tsx','/movements')],
 'T22: apertura1000, expectedAmount1 y countedAmount1 enviados por cliente cierran con diferencia0. T23: ventas no generan movimientos; el controller solo ofrece apertura/cierre. Checkout intenta POST /cash-registers/{id}/movements, ruta inexistente, y silencia el error.',
 'El arqueo puede ocultar faltantes y no permite conciliar ventas, devoluciones, anticipos y pagos. No hay turno financiero consistente.',
 'El servidor calcula saldo esperado desde movimientos inmutables por turno; vincular cada cobro/pago/reembolso y exigir motivo/aprobación para diferencias.',
 'Apertura1000+venta115-devolución15 =>esperado1100; el cliente no puede alterarlo. Caja cerrada rechaza movimientos y doble cierre.')
add('FIN-11','P1','contabilidad','Cuentas históricas y cierre contable carecen de protección efectiva',
 [b('Controllers/ReportsController.cs','accounts.Where(a => a.IsActive)'),b('Controllers/AccountingController.cs','UpdateAccount'),f('pages/accounting/JournalEntriesPage.tsx','firstDayOfCurrentMonth')],
 'Los reportes excluyen cuentas inactivas aunque tengan historia; el tipo de cuenta puede editarse y cambiar signos retroactivamente. La interfaz dice que meses anteriores están cerrados por comparar con el mes actual; no existe entidad ni autorización de períodos cerrados en servidor.',
 'Desaparecen saldos y se puede modificar un período supuestamente cerrado llamando a la API. El primer día local también puede rechazarse por mezclar fecha UTC y medianoche local.',
 'Separar cuentas habilitadas para nuevos asientos de cuentas incluidas en informes. Bloquear cambios estructurales con historia; períodos explícitos, cierre/reapertura autorizados y auditados.',
 'Desactivar cuenta conserva todos sus saldos; API rechaza asientos en período cerrado, permite el primer día de un período abierto y conserva trazabilidad de reapertura.')
add('FIN-12','P2','contabilidad','Catálogo y estados financieros necesitan reglas de consolidación explícitas',
 [b('Controllers/AccountingController.cs','Balance ='),b('Controllers/ReportsController.cs','currentPeriodIncome')],
 'Saldos de cuentas padre se calculan por movimientos propios, no consolidan hijos. Balance general acumula ingresos históricos y usa código3102 para resultado; el catálogo inicial define otra cuenta para utilidad. No se identificó cierre anual que delimite resultado del ejercicio.',
 'La presentación jerárquica induce a interpretar subtotales que no existen; el resultado acumulado puede confundirse con el del período.',
 'Definir cuentas de movimiento y agrupación, roll-up sin doble conteo, resultados del ejercicio/acumulados y saldos de apertura aprobados por contabilidad.',
 'Plan de cuentas con dos niveles y dos ejercicios: conciliación exacta entre mayores, subtotales, resultado y patrimonio.')

add('HOT-01','P1','operacion','Disponibilidad por fechas se mezcla con estado físico actual',
 [b('Database/Repositories/RoomRepository.cs','GetAvailableAsync'),b('Controllers/ReservationsController.cs','room.Status = RoomStatus.Reservada')],
 'T04: una habitación ocupada hoy se excluye de fechas futuras no solapadas. Crear una reserva futura cambia de inmediato el estado global a Reservada. La comprobación de solapamiento y el INSERT son separados.',
 'Se pierde capacidad de venta futura y dos solicitudes concurrentes pueden reservar el mismo intervalo. Un único servidor sigue atendiendo solicitudes concurrentes.',
 'Separar disponibilidad por intervalo y limpieza/mantenimiento físico; bloqueo por habitación o restricción PostgreSQL de exclusión para reservas activas, con límites de fecha [entrada,salida).',
 'Reservas adyacentes permitidas; solapadas concurrentes dejan exactamente una. La reserva del próximo mes no ocupa hoy el cuarto.')
add('HOT-02','P1','operacion','Transiciones de reserva permiten estados imposibles',
 [b('Controllers/ReservationsController.cs','public async Task<ActionResult> Confirm'),b('Controllers/ReservationsController.cs','public async Task<ActionResult> Cancel'),b('Dtos/common/HotelDtos.cs','UpdateReservationRequest')],
 'T05 cancela estadía en curso, T06 confirma la cancelada y T07 acepta editar solamente salida a una fecha anterior a entrada. T39 repetir check-in tras checkout acaba en500 por folio único después de mutaciones.',
 'Habitación libre con huésped alojado, estadías negativas o reservaciones cerradas reabiertas sin reconciliación.',
 'Máquina de estados de servidor y validación del estado final tras aplicar PATCH; transiciones con versión/concurrencia y motivo. Repeticiones idempotentes.',
 'Matriz completa de transiciones válidas/invalidas; ninguna mutación ante una transición rechazada.')
add('HOT-03','P1','operacion','Check-in acepta sobrecapacidad y efectivo incoherente',
 [b('Controllers/ReservationsController.cs','CheckIn('),b('Dtos/common/HotelDtos.cs','CashReceived')],
 'T36: habitación de capacidad2, adultos10, recibido1 y cambio500 se registra con factura total1190. No se liquida el anticipo200. El endpoint no valida de forma completa disponibilidad, capacidad y coherencia del pago.',
 'Sobreocupación y documentos marcados pagados sin el cobro necesario. El anticipo queda como dato sin aplicación contable.',
 'Validar cupo y cuarto en la transacción; calcular cambio y saldo en servidor; registrar anticipo como pasivo/pago aplicado según política contable, nunca confiar en cambio del navegador.',
 'Rechazar10 adultos en cupo2; anticipo200 sobre1190 deja saldo990; recibido1000 produce cambio10 y asientos conciliados.')
add('HOT-04','P1','operacion','Check-out puede cerrar saldos pendientes o volver a facturar la estadía',
 [b('Controllers/ReservationsController.cs','CheckOut('),f('pages/reservations/CheckOutPage.tsx','api.post'),b('Database/Entities/InvoiceEntities.cs','GuestId')],
 'T38: folio1305 se cierra con una sola factura1190 del huésped, dejando consumo sin facturar. El frontend intenta facturar todas las líneas del folio pese a que check-in ya factura hospedaje; no existe vínculo de asignación factura↔línea de folio/reserva.',
 'Riesgo de pérdida de ingresos o doble facturación; consultar por huésped mezcla estadías distintas.',
 'Separar consumos, documentos y aplicaciones de pago; facturar solo líneas pendientes y cerrar cuando saldo/resolución autorizada sea cero. Enlazar factura y reserva/folio.',
 'Estadía1190 facturada al ingreso +extra115: salida emite solo115 y saldo0; reintento no factura otra vez.')
add('HOT-05','P1','operacion','Descuentos y exoneraciones se aplican con reglas divergentes',
 [b('Controllers/ReservationsController.cs','discount'),f('pages/reservations/CheckOutPage.tsx','discount'),b('Dtos/Discount/DiscountDtos.cs','Range')],
 'Código: se suman valores de descuento sin interpretar consistentemente monto fijo/porcentaje, documentación, edad y alcance; se permiten valores de porcentaje superiores a100 en el catálogo. La rama exonerada de check-in vuelve a aplicar descuento sobre una base ya descontada. Checkout usa tasa ||0.15, convirtiendo0 en15%.',
 'Tarifas negativas o reducidas dos veces, impuestos distintos entre pantallas y servidor, descuentos sin evidencia.',
 'Una función de precio autoritativa de servidor, reglas de combinación, tipo, vigencia, elegibilidad y máximo; respuesta de cotización detallada usada por frontend.',
 'Probar descuento fijo50, porcentaje10, acumulación, huésped no elegible, base exonerada y tasa0; misma cotización en reserva, factura y folio.')
add('HOT-06','P1','operacion','Borrado lógico oculta historia relacionada sin un criterio uniforme',
 [b('Controllers/SuppliersController.cs','Delete('),b('Controllers/RoomsController.cs','Delete('),b('Database/ApplicationDbContext.cs','HasQueryFilter')],
 'T21: borrar proveedor oculta compras aún contabilizadas. Relaciones en cascada más filtros lógicos afectan la visibilidad; Folio/FolioItem y Role no tienen los mismos filtros. Los endpoints de clientes y huéspedes sí tienen bloqueos específicos: no se les atribuye este defecto indiscriminadamente.',
 'Historial operativo/fiscal desaparece de consultas mientras sus saldos siguen vivos; habitaciones pueden eliminarse sin la misma protección que clientes.',
 'Desactivar maestros con historia; restringir borrado referencial y definir consultas históricas independientes de la vigencia del maestro. Revisar cada cascada y filtro con pruebas de regresión.',
 'Desactivar proveedor/cuarto conserva compras, reservas, folios y reportes; no quedan referencias invisibles o restauraciones parciales.')
add('INV-01','P1','operacion','Edición de stock evita el kardex y la concurrencia pierde salidas',
 [b('Controllers/InventoryController.cs','CurrentStock ='),b('Controllers/InventoryController.cs','CreateMovement')],
 'T24: PUT de producto fija stock -10 sin movimiento. T40: dos salidas de7 desde10 responden200; saldo final3 aunque el kardex registra14 unidades de salida.',
 'Existencias y movimientos no concilian. Inventario negativo o consumo no descontado aun en una sola máquina.',
 'Stock derivado de movimientos o actualizado atómicamente con condición stock suficiente y versión; ajustes con motivo/usuario, nunca PUT de saldo. Abrir existencias mediante movimiento inicial.',
 'Con10 unidades, dos salidas simultáneas de7 dejan una aceptada y otra conflicto; saldo3, salida total7. Edición directa de saldo prohibida.')
add('INV-02','P2','operacion','Inventario carece de integración de valoración y compras/consumos',
 [b('Database/Entities/ProductEntities.cs','InventoryMovement'),b('Database/Entities/InvoiceEntities.cs','InvoiceItem')],
 'Modelo revisado: cantidades y precios de movimiento, pero sin vínculo sistemático compra→entrada→consumo→costo contable; líneas de factura no identifican producto y no hay unidad/conversión ni método de valoración.',
 'Sirve como registro aislado de cantidades; no permite confiar en costo de venta, inventario valorizado ni reposición conciliada con compras.',
 'Definir alcance requerido: si se gestionan artículos, integrar documentos, unidades, costo promedio/FIFO elegido y asientos. Si es solo control auxiliar, nombrarlo y limitar las promesas.',
 'Compra10 a5, compra10 a7 y salida5 producen existencias y costo según política, conciliados con mayor.')

add('SEG-01','P0','seguridad','Recepción puede convertirse en administrador modificando roles',
 [b('Controllers/RolesController.cs','[Authorize]'),b('Controllers/RolesController.cs','role.Name ='),b('Services/AuthService.cs','ClaimTypes.Role')],
 'T42 reproducido: recepción renombra Admin a AdminAnterior, renombra Recepcion a Admin y refresca su sesión. Recibe rolAdmin y GET /audit-logs devuelve200. El controller de roles solo exige autenticación.',
 'Escalación completa: quien tenga una cuenta operativa puede administrar usuarios y acceder a información reservada. No requiere acceso al sistema operativo.',
 'Políticas de autorización de servidor para administración de roles; identificadores estables de roles privilegiados, nombres no usados como identidad mutable y protección de último administrador. Auditoría y revocación de sesiones al cambiar privilegios.',
 'Recepción y Contador reciben403 en POST/PUT/DELETE roles; no pueden alterar permisos o nombres. Pruebas negativas por cada endpoint administrativo.')
add('SEG-02','P0','seguridad','Credenciales iniciales y clave de firma están publicadas en el proyecto',
 [b('appsettings.json','SecretKey'),('docker/docker-compose.yml','Jwt__SecretKey'),b('Program.cs','Admin123')],
 'T01: el arranque nuevo permite el usuario/contraseña predecibles del seed. appsettings y Compose contienen claveJWT y contraseña de base literales. No se vuelven a copiar esos secretos en este informe.',
 'Si se despliega con esos valores, se puede entrar como admin y la clave de firma conocida permite fabricar tokens. La red local del hotel no constituye autorización.',
 'Rotar secretos, incluir historial del repositorio en la evaluación de exposición; configuración externa protegida, bootstrap de un solo uso y cambio obligatorio de contraseña. Fallar el arranque si sigue la clave de ejemplo.',
 'Instalación limpia no acepta contraseña compartida; tokens firmados con clave anterior se rechazan; escaneo de secretos no encuentra credenciales operativas versionadas.')
add('SEG-03','P1','seguridad','Permisos declarados no se aplican a operaciones sensibles',
 [b('Program.cs','AddAuthorization'),b('Controllers/AccountingController.cs','[Authorize]'),b('Controllers/TaxConfigurationsController.cs','[Authorize]')],
 'T25: recepción crea una cuenta contable y exporta huéspedes; otros endpoints fiscales, inventario, reportes y configuración solo requieren autenticación. Los claims de permisos no tienen políticas que los utilicen. Ocultar menús no protege una API.',
 'Cambios contables/fiscales y extracciones de datos fuera de las funciones del usuario; facilita fraude interno y errores accidentales.',
 'Matriz servidor recurso/acción/rol con denegación por defecto, separando consulta, emisión, ajustes, exportación, configuración y administración. Reutilizarla para navegación y backend.',
 'Tabla de pruebas para anónimo/recepción/contador/admin con401/403/éxito esperado; ningún permiso se sustenta solo en la interfaz.')
add('SEG-04','P1','seguridad','Recepción puede descargar un respaldo completo de PostgreSQL',
 [b('Controllers/BackupController.cs','[Authorize]'),b('Controllers/BackupController.cs','manual')],
 'T41: cuentaRecepcion solicita respaldo manual y recibe200 con el dump. Contiene todas las tablas, incluidas credenciales derivadas y sesiones. T25 también accede a logs de respaldo.',
 'Permite sacar el conjunto completo de datos personales y material de autenticación desde una cuenta operativa.',
 'Restringir generación y descarga a administración autorizada, registrar acceso, retención y almacenamiento cifrado. La exportación operativa debe limitarse al conjunto necesario.',
 'Recepción403 tanto al generar como descargar/listar; respaldo autorizado deja evento de auditoría y no expone rutas internas al resto.')
add('SEG-05','P1','seguridad','Desactivar un usuario no invalida su token de acceso',
 [b('Services/AuthService.cs','Revoke'),b('Program.cs','TokenValidationParameters'),f('store/authStore.ts','logout')],
 'T26: token de usuario desactivado sigue obteniendo200. Revocación afecta refresh tokens; JwtBearer no comprueba estado/version del usuario. Logout del frontend limpia almacenamiento sin llamar a la revocación del backend; axios mantiene Authorization por defecto.',
 'Acceso continúa hasta expiración del token, con ventana aproximada configurada de60 minutos más tolerancia. Roles/usuario en Zustand pueden quedar desactualizados tras refresh.',
 'Versión de sesión/seguridad validada por servidor en operaciones protegidas; invalidar al desactivar, cambiar credenciales o privilegios. Logout revoca sesión, limpia cabeceras y sincroniza pestañas.',
 'Token previo devuelve401 inmediatamente tras desactivación/cambio de seguridad; refresh y pestañas abiertas se actualizan coherentemente.')
add('SEG-06','P1','seguridad','Bitácora declara corrupta su propia cadena sin manipulación',
 [b('Services/AuditService.cs','Timestamp:O'),b('Controllers/AuditLogsController.cs','Timestamp:O'),b('Migrations/20260820161833_InitialPostgresCreate.cs','timestamp without time zone')],
 'T31: verificación falla en primer registro recién creado. Comprobación final reconstruye el hash con2026-09-07T05:35:04.9015318Z; PostgreSQL almacenó2026-09-07T05:35:04.901531. Se pierde precisión de100ns y representación de zona antes de verificar.',
 'Falsas alarmas de manipulación; la cadena no ofrece la garantía que anuncia la interfaz.',
 'Serialización canónica estable antes de persistir y firmar, con precisión PostgreSQL y zona explícita. Probar round-trip de datos antes de habilitar la verificación.',
 'Cadena nueva verifica íntegra después de reiniciar, restaurar y cambiar zona del host; una modificación real se detecta en el registro exacto.')
add('SEG-07','P1','seguridad','La auditoría no es completa ni resistente a modificaciones privilegiadas',
 [b('Services/AuditService.cs','OrderByDescending'),b('Controllers/AuditLogsController.cs','OrderBy'),b('Controllers/RolesController.cs','Update(')],
 'T30 muestra cobertura principalmente de login/facturas mientras cambios de roles, caja, inventario y otros flujos no generan eventos equivalentes. Leer el último hash sin exclusión permite bifurcaciones concurrentes. Misma cuenta de BD tiene permisos para reescribir datos y cadena.',
 'No se puede reconstruir quién cambió datos críticos ni distinguir una reescritura completa. SHA256 por sí solo no es una firma digital ni inmutabilidad frente al administrador de la BD.',
 'Auditar todas las mutaciones críticas y fallos de autenticación; secuencia determinista serializada, almacenamiento append-only con privilegios separados y anclaje/copia externa. No registrar secretos en Changes.',
 'Operaciones simultáneas mantienen una sola cadena; toda acción privilegiada tiene actor, antes/después, hora y correlación. Intento de UPDATE con usuarioAPI es denegado.')
add('SEG-08','P1','seguridad','Puertos y privilegios exponen innecesariamente la base en la LAN',
 [('docker/docker-compose.yml','5432:5432'),('docker/docker-compose.yml','POSTGRES_USER'),('docker/nginx/nginx.conf','listen 80')],
 'Compose publica5432,8080,3000 y80 en todas las interfaces; API usa usuario postgres con contraseña literal. No se configura TLS en el proxy. Include Error Detail=true incrementa detalle de errores de BD.',
 'Equipos de la LAN pueden acceder directamente a servicios internos; HTTP deja expuestos credenciales/tokens al tráfico interceptado. Un fallo de API dispone de permisos de superusuario de BD.',
 'Publicar únicamente proxy en la interfaz administrativa/LAN necesaria, TLS con confianza local, firewall/VLAN separada de huéspedes. UsuarioAPI de mínimo privilegio, migraciones con credencial separada; quitar detalle sensible en producción.',
 'Desde puesto del hotel solo se alcanza proxy;5432/8080 inaccesibles por red. UsuarioAPI no crea roles ni cambia esquema y el login viaja cifrado.')
add('SEG-09','P2','seguridad','Endurecimiento de sesión, archivos y errores incompleto',
 [f('lib/axios.ts','localStorage'),b('Controllers/DocumentAuthorizationsController.cs','AttachmentPath'),b('Program.cs','AddControllers')],
 'Revisión estática: tokens accesibles a JavaScript en localStorage; faltan límites de frecuencia por origen aunque existe bloqueo por fallos de contraseña. Adjunto fiscal valida principalmente extensión, usa datos del CAI en nombre y no explicita tamaño/contenido seguro. No se confirmó escritura por traversal ni XSS explotable.',
 'Amplía impacto de un futuro XSS y facilita consumo de recursos/errores de archivo. Mensajes de validación y500 son inconsistentes para el cliente.',
 'Evaluar sesión con cookie HttpOnly/SameSite y CSRF si se adopta; CSP y límites de login. Nombre aleatorio de adjunto, confinamiento de ruta, tamaño y firmaPDF; ProblemDetails estable sin trazas sensibles.',
 'Archivo inválido/sobredimensionado es rechazado antes de guardar; no sale de Uploads;429 ante abuso y mensajes de error seguros y accionables.')
add('SEG-10','P1','seguridad','Repositorios y lockfiles incluyen material sensible o vulnerable',
 [('frontend/package-lock.json','"node_modules/axios"'),b('hotel-erp.Api.csproj','AutoMapper'),('.gitignore','obj')],
 'npm audit sobre package-lock:10 paquetes afectados (8high,1moderate,1low); NuGet: AutoMapper12.0.1 y Microsoft.OpenApi2.0.0 con avisoshigh. node_modules real usa, por ejemplo, axios1.19.0, router-dom7.18.2 y Vite8.2.1, distintos del lock npm. SQLite versionado contiene1 usuario y7 registros de refresh, sin huéspedes ni facturas; hay logs y bin/obj versionados.',
 'Reinstalar con npm puede introducir versiones distintas de las probadas. El historial conserva credenciales derivadas y artefactos sin procedencia clara. Aviso de dependencia no equivale a explotación demostrada: SSR/RSC/Node-adapter no están expuestos por esta SPA.',
 'Elegir un gestor y lockfile, instalación inmutable, actualizar versiones compatibles y repetir pruebas. Retirar de seguimiento datos/logs/binarios generados conservando respaldo privado; rotar material potencialmente expuesto. Clasificar alcanzabilidad de cada aviso.',
 'Build limpio usa versiones inventariadas, audit no contiene avisos sin evaluación, no se versionan BD/sesiones/logs; ejecutar regresión de mapeos y autenticación tras actualizar.')

add('OPS-01','P0','infraestructura','El frontend actual no compila',
 [f('pages/reservations/CheckInPage.tsx','import { useState'),f('pages/accounting/ChartOfAccountsPage.tsx','const filterAccounts')],
 'npm run build falla con7 diagnósticos de sintaxis en CheckInPage(179–191) y ChartOfAccountsPage(384). Hay imports duplicados/JSX incompleto en CheckIn y falta cerrar handleDeleteConfirm antes de filterAccounts en catálogo. No se usó un bundle viejo para ocultar el fallo.',
 'No puede producirse una versión de frontend desplegable desde el código auditado; entrada de huéspedes y catálogo no se cargan correctamente.',
 'Reparar estructura de ambos archivos y después atender los errores adicionales que puedan aparecer en compilación completa. Exigir build limpio como puerta de entrega.',
 'npm ci con lock elegido, typecheck y build exitosos; navegar check-in y catálogo del artefacto recién construido y completar flujos de prueba.')
add('OPS-02','P0','infraestructura','El Dockerfile de backend copia el código a una ruta distinta de la compilada',
 [('docker/backend/Dockerfile','COPY backend/src/. .'),('docker/backend/Dockerfile','WORKDIR /src/backend/src/hotel-erp.Api')],
 'Verificación de rutas: csproj se copia a/src/backend/src/hotel-erp.Api; COPY backend/src/. . coloca Program.cs en/src/hotel-erp.Api. Build vuelve al primer directorio, que no contiene fuentes. No se ejecutó docker build de extremo a extremo; el fallo se deduce del contexto y las instrucciones COPY.',
 'Construcción nueva del contenedor backend falla por ausencia de código/entrypoint, aunque dotnet build del árbol real funciona.',
 'Alinear rutas de COPY y WORKDIR; .dockerignore para excluir bin/obj/node_modules/docs de laboratorio; construir en contexto limpio y publicar solo salida verificada.',
 'docker compose build --no-cache exitoso desde checkout limpio y API arrancando con DB nueva.')
add('OPS-03','P1','infraestructura','Nginx no entrega la aplicación y cambia las rutas de la API',
 [('docker/nginx/nginx.conf','proxy_pass'),('docker/docker-compose.yml','nginx:'),('docker/frontend/Dockerfile','COPY --from=build')],
 'Proxy exterior sirve/usr/share/nginx/html del nginx estándar sin montar/proxyar frontend. proxy_pass http://api:8080/ dentro de/api/ elimina ese prefijo. Nginx interior del frontend conserva configuración estándar sin fallbackSPA ni proxyAPI.',
 'Puerto80 muestra contenido estándar; llamadas/api llegan con ruta incorrecta. Puerto3000 no resuelve la API relativa y recargar rutas profundas falla.',
 'Una entrada web coherente: proxy exterior al servicio frontend o servir su build en el mismo nginx; preservar/api y habilitar fallbackSPA en el servidor que realmente contiene los assets.',
 'Desde otro equipo LAN: login, llamada/api, navegación y recarga de/invoices funcionan por una única URL.')
add('OPS-04','P1','infraestructura','La impresión usa APIs de Windows dentro del destino Linux',
 [b('Services/RawPrinterHelper.cs','winspool'),b('Services/EscPosService.cs','System.Drawing'),b('Controllers/PrintController.cs','printers')],
 'T27: GET /print/printers devuelve500 en Linux. Build emite CA1416 por APIs disponibles solo en Windows. El Dockerfile final usa ASP.NET Linux.',
 'Facturación no dispone de impresión física soportada en el despliegue previsto. No son advertencias inocuas por usar una sola máquina.',
 'Adaptador de impresión compatible con el host real: colaCUPS, ESC/POS TCP o servicio de impresión Windows separado si el hardware lo exige. Resolver imagen/raster sin System.Drawing Windows-only.',
 'Prueba en la imagenLinux y modelo real de impresora: enumerar, imprimir, reintentar fallo y evitar doble emisión fiscal.')
add('OPS-05','P1','infraestructura','Recibo impreso no conserva fielmente el documento fiscal emitido',
 [b('Services/EscPosService.cs','FACTURA'),b('Controllers/PrintController.cs','FirstOrDefault'),b('Controllers/ReservationsController.cs','new Invoice')],
 'Código: títuloFACTURA genérico, rango inicial fijo, tasa15/4 y datos CAI actuales en vez de snapshot completo; selección de primera reserva del huésped para fechas. T37: factura de check-in carece de FiscalHash/snapshot por el camino usado. Configuración permite ocultar bloques fiscales/totales.',
 'Reimpresión puede diferir del documento original o mostrar otra estadía; notas se presentan como facturas y se omiten datos necesarios.',
 'Snapshot inmutable completo en todas las rutas, plantilla por tipo de documento y datos de la estadía enlazada. Campos obligatorios no configurables como invisibles; validar formato fiscal con responsable autorizado.',
 'Modificar CAI/datos del hotel después de emitir no cambia la reimpresión; nota muestra su tipo y referencia; fechas corresponden a esa reserva.')
add('OPS-06','P1','infraestructura','Respaldo SQL no cubre todos los datos persistentes ni una recuperación del equipo',
 [b('Services/DatabaseBackupService.cs','pg_dump'),('docker/docker-compose.yml','volumes:'),b('Services/BackupOptions.cs','RetentionYears')],
 'Respaldo manual sintético se restauró exitosamente en otra BDlocalPG18 (10 facturas,12 líneas,2 usuarios). No incluye Uploads; Compose no persiste ese directorio ni logsAPI. RetentionYears no tiene implementación efectiva de retención; rclone requiere configuración externa no provista por el repositorio.',
 'Recrear contenedor puede perder adjuntos; daño del disco puede perder base y copia local a la vez. Restaurar SQL no recupera impresora, secretos, adjuntos ni configuración.',
 'Persistir y respaldar adjuntos/configuración, copia cifrada fuera del equipo, retención verificable, monitoreo de espacio y simulacro en máquina reemplazante. Definir RPO/RTO y UPS para el servidor único.',
 'Recuperar en otro equipo desde copia externa: BD+adjuntos+configuración, login, factura, impresión y conciliación; medir tiempo y pérdida máxima de datos.')
add('OPS-07','P2','infraestructura','Procesos de respaldo presentan carreras y fallos de disponibilidad',
 [b('Services/DatabaseBackupService.cs','ProcessStartInfo'),b('Services/BackupHostedService.cs','ExecuteAsync')],
 'Revisión estática: nombres a resolución de segundos, elección del último respaldo global para descarga, procesos externos sin plazo/terminación robusta y salida redirigida sin drenaje completo en todos los caminos. Reintentos limitados a primeros pendientes pueden postergar otros; errores no contenidos en servicio hospedado pueden detener API. Hora02:00 depende del host/contenedor.',
 'Dos respaldos pueden colisionar o devolver archivo equivocado; procesos trabados, disco lleno o nube inaccesible pueden afectar disponibilidad. Estas carreras no fueron reproducidas como carga destructiva.',
 'Identificador único por ejecución, exclusión de trabajos, descargar por id, timeout/kill y drenaje asíncrono; capturar errores por ciclo, backoff y cola justa. Zona explícita y compatibilidad pg_dump/servidor.',
 'Simular nube caída, proceso colgado, disco lleno y dos solicitudes; API sigue disponible y cada resultado corresponde al trabajo correcto.')
add('OPS-08','P2','infraestructura','Salud y arranque no reflejan preparación real',
 [b('Program.cs','/health'),b('Program.cs','Database.Migrate()')],
 'T43 obtiene200 del endpoint de salud; el código devuelve un literal sin consulta a BD. Migraciones y seed corren al inicio; catchfatal no garantiza una salida no-cero explícita. Compose espera DBhealthy pero no unaAPIready.',
 'Monitoreo puede anunciar servicio sano sin capacidad operativa y un fallo de migración puede producir bucles poco diagnósticos.',
 'Separar liveness/readiness; readiness con conexión/migración y dependencias esenciales. Migraciones controladas con respaldo previo y señal clara de fallo; healthchecks de API/proxy.',
 'Cortar BD hace readiness503 sin matar liveness; migración fallida sale con error visible y no acepta solicitudes.')
add('DB-01','P1','infraestructura','Correlativo protegido por lock sigue usando el CAI obsoleto de EF',
 [b('Database/Repositories/InvoiceRepository.cs','GetNextCorrelativeAsync'),b('Services/PostgresCorrelativeLock.cs','pg_advisory_xact_lock'),b('Controllers/InvoicesController.cs','GetByIdAsync')],
 'T28: dos solicitudes que precargaron el mismo CAI esperan el lock; una responde201 y la otra500 por correlativo único. EF conserva valores de la entidad ya seguida dentro del contexto. El índice único sí evitó dos facturas con igual número.',
 'Errores bajo concurrencia normal y reintentos inciertos. No se atribuye el mismo fallo automáticamente al servicio de DocumentAuthorization que carga dentro del lock.',
 'Actualizar/retornar secuencia atómicamente en BD o recargar la entidad dentro del lock y transacción del documento. Claves de idempotencia y respuesta409/reintento controlado.',
 'Solicitudes concurrentes producen números únicos consecutivos válidos y ninguna500 por carrera; rango agotado no permite emisión.')
add('DB-02','P1','infraestructura','Inicio de rango y validación de autorización son inconsistentes',
 [b('Controllers/CAIController.cs','CurrentCorrelative ='),b('Controllers/DocumentAuthorizationsController.cs','CurrentCorrelative ='),b('Services/FiscalAuthorizationService.cs','sequential')],
 'T08: rango inicial...00000001 emite primero...00000002, porque CurrentCorrelative inicia en InitialRange y luego se incrementa. Validación textual no garantiza igualdad de prefijos/tipo entre límites y no aplica de manera uniforme fecha inicial/final.',
 'Se omite el primer comprobante autorizado y pueden aceptarse rangos incoherentes o autorizaciones fuera de vigencia.',
 'Guardar siguiente número o último emitido con semántica inequívoca; parseo estructurado de rango, prefijo y tipo; validación uniforme de vigencia por fecha fiscal local.',
 'Primer número coincide con inicio; último se emite una sola vez; rango invertido, prefijos diferentes o fuera de vigencia son rechazados.')
add('DB-03','P1','infraestructura','Mezcla de UTC, hora hondureña y límites de consulta',
 [b('Services/HondurasTime.cs','Add(Offset)'),b('Database/Repositories/InvoiceRepository.cs','i.InvoiceDate <= end'),b('Controllers/ReportsController.cs','to.AddDays(1)')],
 'Se resta6 horas a DateTimeUtc conservando KindUtc; una hora local se serializa conZ como si fuera UTC. Hay columnas timestamp without time zone y comprobaciones de vencimientoUTC frente a fecha local. Reportes usan <=día siguiente incluyendo medianoche siguiente; listado de facturas puede recibir día final a00:00 y excluir su jornada.',
 'Documentos caen en fecha/período equivocado y CAI puede vencerse antes de finalizar el día en Honduras. Rangos adyacentes pueden contar la misma medianoche.',
 'UTC para instantes reales, DateOnly para fecha fiscal/estadía, zonaAmerica/Tegucigalpa al presentar; intervalos [inicio,finExclusivo) y contrato de fechas documentado.',
 'Probar23:59/00:00 local, último día fiscal y cambio de mes: cada factura pertenece exactamente a un período y CAI vence al momento definido.')
add('DB-04','P1','infraestructura','DTO devuelven líneas vacías y autorizaciones fallan al mapear fechas',
 [b('Dtos/Mappings/MappingProfile.cs','CreateMap<Invoice,'),b('Dtos/Mappings/MappingProfile.cs','CreateMap<DocumentAuthorization,'),b('Dtos/common/InvoiceDtos.cs','DocumentAuthorizationDto')],
 'T34: ItemsAPI vacío con líneas presentes en BD; InvoiceItems/FolioItems/PurchaseInvoiceItems no se enlazan explícitamente aItems y falta mapa de línea de factura. T17/T35: autorización se inserta pero mapear DueDate DateOnly→DateTime produce500; listar también falla con una fila.',
 'Detalles y liquidación muestran cero o sin consumos, no se pueden operar autorizaciones y reintentar creación puede duplicar datos.',
 'Mapas explícitos de colecciones y tipos de fecha consistentes; pruebas AssertConfigurationIsValid más round-trip DTO con entidad real y navegación cargada.',
 'Crear/listar/detallar cada documento devuelve todas sus líneas y totales; autorizaciónDateOnly crea201 y lista200 sin escrituras parciales.')
add('DB-05','P2','infraestructura','Listados completos y N+1 de reportes crecerán con el historial',
 [b('Database/Repositories/InvoiceRepository.cs','GetAllAsync() => await _context.Invoices'),b('Controllers/ReportsController.cs','foreach (var account'),f('pages/inventory/InventoryPage.tsx','Promise.all')],
 'Medición local:10 facturas=10.082bytes y8–16ms;5.010=4.998.975bytes y397–562ms, incluso sin líneas nuevas. Reportes hacen una consulta por cuenta y filtran fechas en memoria. Listados incluyen entidades/navegaciones completas y paginan en navegador; inventario carga productos/categorías/movimientos aun en pestaña no visible.',
 'Memoria, tráfico y espera crecen por años de operación; varias recepciones compiten con backups y reportes en el mismo servidor. Estas cifras no son un SLA del hardware del hotel.',
 'Paginación/filtrado/orden estable enSQL, DTO proyectado con AsNoTracking para lectura, agregación por cuenta y período enSQL; carga de detalle bajo demanda y cancelación de solicitudes.',
 'Con50mil documentos, listado devuelve solo página50 y tamaño acotado; estado de resultados usa consultas constantes y mediana/p95 acordados medidos en equipo destino.')
add('DB-06','P2','infraestructura','Esquema requiere restricciones e índices de negocio',
 [b('Database/ApplicationDbContext.cs','OnModelCreating'),b('Migrations/20260820161833_InitialPostgresCreate.cs','CreateIndex')],
 'EXPLAIN local consulta24 facturas por día y escanea5.010, descarta4.986 (5.34ms,1002 buffers). Faltan índices de fecha compuestos alineados con consultas. No hay exclusión temporal de reservas ni protección de stock concurrente; importes numeric sin escala fija/checks suficientes.',
 'Validación exclusivamente de aplicación permite corrupción por carreras y entradas fuera de dominio; consultas históricas escalan linealmente.',
 'Diseñar índices a partir de consultas reales (fecha/id, estado/fecha, cuarto/intervalo), restricciones de integridad y precisión por dominio. Medir planes antes/después; no añadir índices indiscriminadamente.',
 'EXPLAIN con volumen real confirma menoslecturas; restricciones rechazan fechas/importes/solapamientos inválidos incluso medianteSQL directo.')
add('DB-07','P2','infraestructura','PostgreSQL ya está implementado, pero falta validar migración de datos históricos',
 [b('Program.cs','UseNpgsql'),b('Migrations/20260820161833_InitialPostgresCreate.cs','InitialPostgresCreate'),b('Data/hotel.db','')],
 'El código actual usa Npgsql y migración inicialPostgreSQL; arrancó y migró correctamente enPG18.6 del laboratorio. Compose especificaPG16, no ejecutado aquí. Sigue versionada unaSQLite con4 migraciones y datos de usuario/sesiones; no hay procedimiento de transferencia y conciliación de un hotel existente.',
 'Confundir base nueva con migración exitosa puede dejar atrás historia o usar datos/bundle anteriores. El arranque enPG18 no certifica la imagenPG16.',
 'Definir origen oficial, exportación transformada, saldos iniciales, secuencias fiscales, identidades y conteos/conciliación; ensayo con copia protegida. Probar la versiónPG16 exacta o actualizar el objetivo de manera deliberada.',
 'Ensayo de migración con documentos reales anonimizados conserva totales, líneas y correlativos; plan de rollback documentado y aprobado por responsables.')

add('UX-01','P1','frontend','Menú fijo impide trabajar con comodidad en pantallas estrechas',
 [f('components/layout/Sidebar.tsx','w-64'),f('components/layout/AppLayout.tsx','ml-64')],
 'Captura dashboard-movil.png: sidebar abierto de256px en viewport390px; mediciónDOM previa width390, scrollWidth575. No hay drawer/breakpoint del shell; el contenido queda comprimido y desborda. Sí existen breakpoints dentro de numerosas páginas.',
 'El personal que use tableta/móvil o zoom pierde acceso legible a operaciones. Afecta reflowWCAG1.4.10; no se hizo certificación completaWCAG.',
 'Menú como drawer en anchuras pequeñas, cerrado inicialmente y con foco controlado; main sin margen fijo cuando sea overlay. Mantener tablas con desplazamiento propio y acciones visibles. $impeccable adapt.',
 '320/390/768px y zoom200%: sin scroll horizontal de toda la página, navegación y tareas esenciales accesibles.')
add('UX-02','P1','frontend','Contraste insuficiente en botones y estados de habitación',
 [f('index.css','C69C4B'),f('pages/admin/AuditLogsPage.tsx','text-white'),f('pages/rooms/FloorMapPage.tsx','text-white')],
 'Cálculo sRGB del color sólido blanco sobre#C69C4B y sobre verde#22c55e/amarillo#eab308 del mapa queda por debajo de4.5:1 para texto normal. El mapa muestra estado principalmente como color antes de abrir detalle; faltan estadosLimpieza/Bloqueada en su leyenda.',
 'Baja legibilidad para recepcionistas con visión reducida y ambigüedad de estado para daltonismo. WCAG1.4.3 y uso del color1.4.1.',
 'Tokens accesibles por tema y pareja fondo/texto con contraste comprobado; estado textual en cada cuarto, leyenda completa. Preservar dorado como acento de marca. $impeccable colorize.',
 'Texto normal≥4.5:1 y grande≥3:1 en ambos temas; estado comprensible en escala de grises.')
add('UX-03','P1','frontend','Etiquetas, iconos y selección contable no son accesibles por teclado',
 [f('pages/auth/LoginPage.tsx','<label'),f('pages/accounting/JournalEntriesPage.tsx','function AccountCombobox'),f('components/ui/Pagination.tsx','<select')],
 'Labels visuales sin htmlFor/id en login y muchos formularios; mostrarcontraseña/cerrar modales sin nombre accesible. AccountCombobox usa div/onMouseDown sin roles, tabIndex ni manejo de teclado. Selector de tamaño de página no tiene etiqueta asociada.',
 'Dificulta lectores de pantalla y puede impedir crear asientos sin ratón. Relacionado conWCAG1.3.1,2.1.1,4.1.2.',
 'Controles nativos o combobox accesible, ids/labels y errores asociados; nombres y estadosaria de iconos. Foco visible también en acciones que hoy aparecen solo con hover. $impeccable harden.',
 'Crear asiento, autenticar y paginar usando soloTab/ShiftTab/Enter/Escape, con lector de pantalla anunciando nombre, error y estado.')
add('UX-04','P1','frontend','Modales carecen de un patrón consistente de foco y cierre',
 [f('components/ui/ConfirmDialog.tsx','role="dialog"'),f('components/layout/CommandPalette.tsx','autoFocus'),f('pages/rooms/FloorMapPage.tsx','selectedRoom &&')],
 'ConfirmDialog tiene role y título, pero no trap/retorno de foco ni Escape; múltiples modales manuales ni siquiera tienen semántica dialog. Fondo clicable puede cerrar durante envío. Paleta sí manejaEscape, pero no establece navegación completa de opciones.',
 'Foco puede ir a controles de fondo; cierre inesperado pierde contexto o hace dudar si una operación terminó. PatrónAPG dialog yWCAG2.4.3/4.1.2.',
 'Un componente de diálogo accesible común: foco inicial/encerrado/retorno, Escape según estado, inert del fondo y bloqueo coherente en envío. $impeccable harden.',
 'Abrir/cerrar vuelve al disparador; Tab no sale al fondo; la operación en curso no queda sin estado visible.')
add('UX-05','P1','frontend','Botones de notas, cargos y autorizaciones no coinciden con el contratoAPI',
 [f('pages/invoices/InvoicesPage.tsx','/invoices/credit-note'),f('pages/folios/FoliosPage.tsx','/items'),f('pages/invoices/AuthorizationsPage.tsx','toISOString')],
 'Código: nota crédito usa ruta/invoices/credit-note en vez de/invoices/{id}/credit-note. Añadir consumo desdeFolios/Checkout omiteFolioId requerido porDTO. Autorización envía timestampISO aDateOnly; abrirPDF conwindow.open no aporta bearer. Source=document se filtra luego como docauth. La ruta de anulación visible está rechazada por diseño del backend.',
 'Acciones principales fallan aun después de corregir compilación, con mensajes genéricos; bloquea cargos/notas/gestión fiscal. Hay evidenciaAPI independiente enDB-04.',
 'Contrato único tipado generado desdeOpenAPI; pruebas de integración por acción de pantalla. Eliminar acciones incompatibles y descargar adjuntos vía cliente autenticado. $impeccable harden.',
 'Cada botón produce la ruta/cuerpo esperado; cargo queda visible, nota se vincula al original, PDF abre autorizado y filtrodeautorización funciona.')
add('UX-06','P1','frontend','Checkout calcula y envía datos fiscales incompatibles',
 [f('pages/reservations/CheckOutPage.tsx','Empresa'),f('pages/reservations/CheckOutPage.tsx','0.15'),f('pages/reservations/CheckOutPage.tsx','catch')],
 'TaxpayerType=Empresa al introducirRTN no coincide con valores admitidos por backend. Se envían propiedades de pago no soportadas porCreateInvoiceDTO; cálculos duplicados ignoran descuentos de línea o interpretan fijo como porcentaje. Error del movimiento de caja se descarta. Selección de nuevo huésped no reinicia todo el estado de pago/RTN.',
 'Cobro rechazado para empresas o con impuestos/caja erróneos, y posible aplicación de datos del huésped anterior.',
 'Cotización y liquidación autoritativas de servidor con DTO compartido; limpiar estado por reserva y tratar flujo como una única operación con resultado integral. $impeccable harden.',
 'Empresa conRTN, consumidor, exento, descuento fijo y cambio dehuésped: importes correctos, sin datos arrastrados ni éxito parcial.')
add('UX-07','P1','frontend','La acción de ingresar una reserva no conserva la reserva seleccionada',
 [f('pages/reservations/ReservationsPage.tsx',"navigate('/checkin')"),f('pages/reservations/CheckInPage.tsx','/reservations')],
 'La lista navega/checkin sin identificador; wizard crea otra reserva. El guardado de huésped puede fallar y aun avanzar. No hay implementación completa del calendario que promete el acceso deDashboard.',
 'El recepcionista pierde la reserva preparada, repite datos o genera otra operación; mayor demora justo en llegada.',
 'Ruta/checkin?reservationId=... oparam explícito, precargar reserva y actualizar la misma; avanzar solo después de validación/guardado exitoso. Mostrar agenda real o nombrar vista como lista. $impeccable shape.',
 'Desde reserva existente el check-in conservaId, cuarto, fechas y anticipo, no crea otra y permite volver sin perder datos.')
add('UX-08','P1','frontend','Errores y cargas se presentan como cero, vacío o éxito',
 [f('pages/accounting/TrialBalancePage.tsx','finally'),f('pages/reports/ReportsPage.tsx','taxSummary?.'),f('pages/invoices/InvoiceEditPage.tsx','!invoice'),f('pages/settings/SettingsPage.tsx','catch')],
 'TrialBalance no captura error de carga; inicial vacío puede parecer balancecero. Reportes muestran0 antes de tener respuesta y retienen resultadosanteriores al cambiar filtros. InvoiceEdit oculta alerta cuando!invoice y quedaCargando. Settings silencia cargas y no presenta todos los fallos de guardado.',
 'Cero se interpreta como ausencia de obligaciones y el usuario puede trabajar con resultadosdeotroperíodo o configuración no guardada.',
 'Estados explícitos cargando/sin datos/error/desactualizado/guardado; mostrar período aplicado a losresultados, invalidar o marcar al cambiar filtros. Mantener datos solo si se indica suantigüedad. $impeccable harden.',
 'Simular500, desconexión y respuesta lenta por pantalla: nunca se anuncia cero/éxito sin confirmación y siempre hay reintento.')
add('UX-09','P2','frontend','Búsquedas y paginación generan carreras y trabajo innecesario',
 [f('pages/guests/GuestsPage.tsx','[search]'),f('pages/customers/CustomersPage.tsx','[search]'),f('lib/axios.ts','axios.create')],
 'Búsqueda lanza solicitud por cada carácter, sin debounce/cancelación y luego vuelve a filtrar localmente. Respuestas fuera deorden pueden sobrescribir resultados. Paginación local con valorinicial15 mientras selectorofreceotros tamaños. ClienteHTTP no define timeoutglobal.',
 'Resultados saltan, aumenta carga deBD y la UIpuede esperar indefinidamente. Listar15filas no significa descargar15registros.',
 'Debounce y AbortSignal, paginación real de servidor y filtros coherentes, reset/clamp de página. Usar ReactQuery ya instalado para caché, invalidación y estados. $impeccable optimize.',
 'Escribirconsulta rápida con respuestas reordenadas deja solo resultadosdeúltimaconsulta; ninguna página queda vacía fuera de rango.')
add('UX-10','P2','frontend','Navegación, permisos y atajos prometidos no están alineados',
 [f('components/layout/CommandPalette.tsx','Enter'),f('components/layout/Breadcrumbs.tsx','pathname'),f('App.tsx','guests'),f('components/layout/Sidebar.tsx','Contador')],
 'Paleta anunciaEnter para seleccionar pero carece de manejador de selecciónporEnter/flechas. Búsqueda literal no encuentrafacturas conel texto deejemplo si el módulo se llamaFacturación. Breadcrumbs genera padres/accounting y detalles no declarados. MenúHuéspedes permiteContador mientras ruta loexcluye. Los aliasRecepcion/Recepcionista sí se normalizan: no son el fallo.',
 'Atajos poco fiables, enlaces404 y acciones que terminan en denegación; fricción repetida en trabajo diario.',
 'Una definición de rutas/metadatos/permisos para menú,paleta,breadcrumb; búsqueda por sinónimos y teclado completo. $impeccable clarify.',
 'Todos los enlaces corresponden a rutas válidas y permisos;facturas encuentraFacturación; flechas yEnter abren opción correcta.')
add('UX-11','P2','frontend','Acciones duplicables y validación inconsistente entre capas',
 [f('pages/discounts/DiscountsPage.tsx','const save'),f('pages/rooms/RoomTypesPage.tsx','const save'),f('pages/users/UsersPage.tsx','length < 6')],
 'Varios formularios permiten pulsarGuardar varias veces sin indicador/disabled (descuentos,tipos,habitaciones,caja). Usuario acepta contraseña6caracteres enfrontend frente a8enbackend. Errores esperanresponse.data.message aunque API devuelve string oValidationProblemDetails.',
 'Duplicados, errores genéricos y retrabajo; la indicación visual de guardado varía de un módulo a otro.',
 'Estado de envío uniforme, idempotencia de servidor para operacionesfinancieras, restricciones compartidas y traducción centralizada de errores. $impeccable harden.',
 'Doble clic no duplica; contraseña inválida se explica antes/después; errorsDTO resaltan el campo correspondiente.')
add('UX-12','P2','frontend','La interfaz afirma funciones que no están implementadas o comprobadas',
 [f('pages/reports/ReportsPage.tsx','Descargar Formato Guía PDF'),f('pages/admin/AuditLogsPage.tsx','Registro inmutable'),f('pages/dashboard/DashboardPage.tsx','tiempo real')],
 'BotónDescargar Formato GuíaPDF no tieneonClick. Copyinmutable contradiceSEG-06/07. Dashboard/mapa se actualizan bajo solicitud, no por canal en tiempo real. CasillasDMR-1 están codificadas sin fuente/versionado fiscal enproyecto.',
 'Personal confía en controles o actualizaciones inexistentes; la guía fiscal puede confundirse con declaración validada.',
 'Implementar acción o retirarla; indicar última actualización y límites reales de bitácora/reportes. Revisar formatoDMR-1 con documentación oficial y contador; mostrar borrador yversión. $impeccable clarify.',
 'Ningún botóninactivo sin explicación; descripciones coinciden con comportamiento y reportes muestran fecha/período/fuente aplicados.')
add('UX-13','P2','frontend','Previsualización de impresión ignora cambios aún no guardados',
 [f('pages/settings/SettingsPage.tsx','preview'),b('Controllers/PrintController.cs','preview')],
 'Efecto observa numerosas opciones pero petición depreview solo lleva ancho; APIusa configuraciónpersistida. Cambiarlogo/font/visibilidad puede mostrar recibo distinto de loque se guardará. LogoBase64 no tiene límite explícito y borrarlogo no se expresa deformauniforme.',
 'Configuración de impresora por ensayo/error y resultados engañosos; logo grande incrementa cargas.',
 'Preview puro que reciba todas las opciones no guardadas o aclarar que muestra la última configuración guardada; límites/dimensiones de logo y eliminación explícita. $impeccable harden.',
 'Cambiar cada opción produce la previsualización esperada antes de guardar; imprimir coincide con ella dentro de restricciones deldispositivo.')
add('UX-14','P2','frontend','Tokens y movimiento necesitan una política común',
 [f('index.css','@import'),f('pages/common/NotFoundPage.tsx','animate-bounce'),f('App.tsx','Suspense')],
 'Tokens claros/oscuros existen, pero muchos componentes fuerzan#C69C4B/textwhite y tamaños11px. No hay prefers-reduced-motion. FuenteGoogle externa añade dependencia de internet enproductoLAN. Suspense global sustituye todo el shell al cargar un módulo y falta ErrorBoundary de ruta.',
 'Tema oscuro pierde coherencia; usuarios sensibles al movimiento no tienen alternativa; fallos dechunk pueden dejar pantalla sinrecuperación.',
 'Tokens semánticos, fuente local confallback, movimiento reducido que conserve feedback y boundaries/skeletons porcontenido. $impeccable optimize y $impeccable harden.',
 'Ambostemas, sininternet, reduce-motion ychunkfallido: texto legible, shell estable yreintento accesible.')
add('QA-01','P1','calidad','No hay una puerta automatizada que impida entregar código roto',
 [('frontend/package.json','"scripts"'),('frontend/eslint.config.js','defineConfig'),b('hotel-erp.Api.csproj','TargetFramework')],
 'Buildfrontend falla y ESLint arroja93problemas:88errores/5warnings. No se encontró suite de pruebas de producto ni pipelineCI versionado. Backend compila con10warnings/0errores usando paquetes existentes. Los scripts de esta auditoría son reproducciones, no una suite previa del producto.',
 'Regresiones sintácticas, contractuales y financieras llegan al despliegue; aprobar porque backendcompila no validaERP.',
 'CI conrestoreinmutable, build, lint y pruebasdeinvariantes: asientos, impuestos, idempotencia, permisos, concurrencia ycontratosUI. Priorizar pruebas reales sobre snapshots que repitan implementación.',
 'PR con error de sintaxis, permisoabierto o asiento incorrecto fallaCI; versión desplegada tieneSHA y resultados verificables.')
add('QA-02','P2','calidad','Lógica de negocio duplicada en controladores y navegador',
 [b('Controllers/InvoicesController.cs','SubTotal'),b('Controllers/ReservationsController.cs','SubTotal'),f('pages/reservations/CheckOutPage.tsx','subtotal')],
 'Cálculos y persistencia repartidos encontrollers extensos, repositoriosconSaveChanges y formulasTypeScript. DTO compartidos entre lectura/escritura permiten campos derivados y estadosdébiles. TaxService no concentra todoslos caminos.',
 'Cada corrección puede dejar compras/notas/check-in/checkout divergentes; transacciones y testeo resultan difíciles.',
 'Casos deuso para emitir, acreditar, liquidar, ajustarstock ycerrarcaja; dominio monetario común, DTO deentrada/salida distintos ycontroladoresdelgados. Refactorizar después de fijar pruebasdecomportamiento.',
 'Una misma cotización produce resultados idénticos en todoslos flujos y las pruebas ejercitan el servicio de negocio sin navegador.')
add('QA-03','P2','calidad','Falta trazabilidad de entrega y manual de operación local',
 [('frontend/README.md','#'),('docs/QA_FIX_PLAN.md','#'),('.codex/hooks.json','C:')],
 'READMEfrontend conserva contenido base; planes/auditorías previos no acreditan el build actual. Se versionan doslockfiles, bundles/binarios generados y hooks a rutasWindows personales. No hay runbook completo de instalación, actualización, recuperación eimpresora para el hotel.',
 'Otra persona puede ejecutar artefacto antiguo o perderdatos alactualizar. Los hooks de una máquina no sustituyenCI ni son por sí mismos indicio de malware.',
 'Manifiesto de release concommit/imágenes/migraciones, unrunbook probado y configuración portable. Vincular correcciones a evidencia nueva, mantener auditorías previas como historia.',
 'Técnico distinto instala y recupera usando solo documentación y secretosentregados de forma segura; puede identificar la versión enejecución.')

add('FIN-13','P1','contabilidad','La API permite facturas con líneas e importes negativos',
 [b('Dtos/common/InvoiceDtos.cs','public record InvoiceItemDto'),b('Controllers/InvoicesController.cs','request.Items')],
 'T12: factura normal con cantidad1 y precio -100 obtiene201. El DTO de líneas usado para entrada carece de validaciones monetarias equivalentes a otros módulos; balancear un asiento con importes negativos no valida su significado.',
 'Puede reducir caja/ingresos mediante una factura ordinaria sin pasar por autorización, motivo y referencia de una nota de crédito.',
 'DTO de entrada específico con cantidad positiva, precio no negativo, descuento acotado, tasas permitidas y precisión definida; correcciones negativas mediante eventos/documentos autorizados.',
 'Factura con precio/cantidad negativos, descuento superior a100 o tasa fuera de dominio devuelve400 sin consumir correlativo ni guardar asiento.')

add('FIN-14','P1','contabilidad','El borrador DMR-1 muestra un cálculo de ISV sin modelo de retenciones',
 [f('pages/reports/ReportsPage.tsx','Período Fiscal DMR-1'),b('Controllers/ReportsController.cs','TaxSummaryDto')],
 'La pestaña DMR-1 reutiliza tax-summary (ventas, ISV de compras y tasa turística), con casillas30/40/55/70 codificadas. No tiene detalle de retenciones por tercero/concepto. El SAR describe la DMR como información de retenciones efectuadas a terceros: [SAR, DMR](https://www.sar.gob.hn/dmr/). La discrepancia de alcance está confirmada; no se certifica aquí un formulario sustituto ni las obligaciones particulares del hotel.',
 'Un usuario puede confundir el resumen de ISV con la declaración de retenciones y trasladar valores a un trámite fiscal equivocado.',
 'Retirar la identificación DMR-1 hasta implementar y validar el formulario vigente aplicable. Separar ISV, compras y retenciones, con versión de normativa, período y conciliación por tercero; revisar junto al contador del hotel.',
 'El responsable contable compara una declaración completa con la guía oficial vigente; campos, bases y retenciones se corresponden con sus documentos, sin reutilizar un resumen de otro impuesto.')

def link(loc):
    return f"[{loc['archivo']}:{loc['linea']}]({ROOT/loc['archivo']}:{loc['linea']})"
def render(r):
    return f"### {r['id']} · {r['prioridad']} · {r['titulo']}\n\n**Ubicación:** "+'; '.join(map(link,r['ubicaciones']))+f"\n\n**Evidencia y alcance:** {r['evidencia']}\n\n**Impacto:** {r['impacto']}\n\n**Corrección propuesta:** {r['recomendacion']}\n\n**Criterio de cierre:** {r['aceptacion']}\n\n"

sections=[('01-contabilidad-fiscal.md','Contabilidad y consistencia fiscal','contabilidad'),('02-operacion-hotel-inventario.md','Operación hotelera e inventario','operacion'),('03-seguridad.md','Seguridad, permisos y privacidad','seguridad'),('04-postgresql-docker-rendimiento.md','PostgreSQL, Docker, impresión, respaldo y velocidad','infraestructura'),('05-frontend-experiencia.md','Frontend y experiencia de usuario','frontend'),('06-calidad-codigo.md','Calidad y mantenibilidad','calidad')]
for filename,title,area in sections:
    selected=[r for r in records if r['area']==area]
    (OUT/filename).write_text('# '+title+'\n\n'+''.join(render(r) for r in selected))
(OUT/'hallazgos.json').write_text(json.dumps(records,ensure_ascii=False,indent=2))
counts=collections.Counter(r['prioridad'] for r in records)
print('Hallazgos',len(records),dict(counts))
