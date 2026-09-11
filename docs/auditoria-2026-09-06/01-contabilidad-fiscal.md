# Contabilidad y consistencia fiscal

### FIN-01 · P0 · Las notas de crédito aumentan ingresos en lugar de revertirlos

**Ubicación:** [backend/src/hotel-erp.Api/Services/AccountingService.cs:17](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AccountingService.cs:17); [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:253](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:253)

**Evidencia y alcance:** Reproducido T09: factura base L100 y nota base L100 producen dos créditos positivos a ingresos. El ISV de la nota fue cero por FIN-03; aun así el error de signo del asiento queda demostrado. La original se marca Anulada sin reversar su asiento.

**Impacto:** El saldo de ingresos llega a L200 cuando la devolución total debería neutralizar L100. Una nota parcial también anula la factura completa; no hay control acumulado del importe ya acreditado.

**Corrección propuesta:** Modelar factura, nota de crédito y débito como eventos distintos; invertir el asiento de la nota de crédito, enlazar líneas de origen y limitar el acumulado acreditado. Mantener el documento original y su historia.

**Criterio de cierre:** Factura 100+15; nota total 100+15: ingreso e ISV netos cero, devolución/cuenta por pagar conciliada. Dos notas parciales nunca exceden el saldo disponible.

### FIN-02 · P0 · Las operaciones se guardan parcialmente antes de devolver un error

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:176](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:176); [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:131](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:131); [backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:41](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Repositories/InvoiceRepository.cs:41)

**Evidencia y alcance:** T03: check-in sin CAI devuelve 400, pero deja reserva CheckIn, habitación ocupada y folio. T32–T33: factura de L0.04 queda persistida sin asiento tras un 500. Correlativo, factura, asientos y bitácora se guardan en pasos separados.

**Impacto:** Reintentar después del error puede duplicar documentos, consumir correlativos o dejar estadías y contabilidad inconsistentes. Una transacción interna de SaveChanges no abarca el caso de uso completo.

**Corrección propuesta:** Una transacción de PostgreSQL por operación de negocio, con validaciones previas y clave de idempotencia. Correlativo, documento, folio, pago, asiento y evento de auditoría deben confirmar juntos; impresión y nube van después mediante cola persistente.

**Criterio de cierre:** Inyectar fallo en cada paso y comprobar rollback completo. Repetir la misma clave devuelve el mismo resultado y no crea otra venta.

### FIN-03 · P1 · Las notas calculan impuestos desde LineTotal suministrado por el cliente

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:310](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:310); [frontend/src/pages/invoices/InvoicesPage.tsx:173](/home/vm/Projects/erp-hotel/frontend/src/pages/invoices/InvoicesPage.tsx:173)

**Evidencia y alcance:** T09: cantidad 1, precio 100 y tasa 15%, sin LineTotal en JSON, generan subtotal 100 e ISV0. La interfaz construye líneas sin ese campo. El subtotal se recalcula por un camino y el impuesto por otro.

**Impacto:** El importe de impuestos depende de un campo manipulable o ausente; también afecta notas de débito.

**Corrección propuesta:** Calcular base, descuentos e impuestos exclusivamente en servidor a partir de cantidades y precios autorizados; eliminar totales derivados de los DTO de entrada.

**Criterio de cierre:** Omitir, falsear o enviar LineTotal=999 no cambia el resultado correcto de la nota.

### FIN-04 · P1 · Desglose fiscal de tasas y bases incorrecto

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:213](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:213); [backend/src/hotel-erp.Api/Controllers/PurchaseInvoicesController.cs:81](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/PurchaseInvoicesController.cs:81); [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:72](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:72)

**Evidencia y alcance:** T13: base 100 a 18% guarda ISV15Amount=18 e ISV18Amount=0. T14: línea exenta100 guarda TaxableAmount100 y ExemptAmount0. El resumen de base 15 suma todo el subtotal de documentos con ISV15 positivo, incluso documentos mixtos.

**Impacto:** Los libros y resúmenes no representan las bases gravadas, exentas y exoneradas reales. No se está dictaminando la tasa legal aplicable a cada producto: falla incluso la clasificación de la tasa solicitada.

**Corrección propuesta:** Motor fiscal único por línea, con base y tasa explícitas, redondeo definido y agregados separados. Usarlo en ventas, compras, notas, hospedaje y reportes.

**Criterio de cierre:** Documento mixto con bases exenta100, 15%100 y 18%100: cada casilla debe coincidir con sus líneas; validar exoneración global y parcial.

### FIN-05 · P1 · Redondeo monetario inconsistente rompe la partida doble

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:117](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:117); [backend/src/hotel-erp.Api/Services/AccountingService.cs:78](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AccountingService.cs:78)

**Evidencia y alcance:** T32: base 0.03 con 15% y 4%: componentes redondeados0.03+0+0 frente a total 0.04. AccountingService rechaza el asiento después de guardar la factura. T15 fue un control negativo: su primer ejemplo sí pasó, no constituye reproducción del defecto.

**Impacto:** Importes legítimos de centavos pueden fallar y dejar documentos huérfanos; otras rutas redondean de forma distinta.

**Corrección propuesta:** Definir una política monetaria única y hacer que el total sea la suma de componentes contabilizados, con ajuste explícito de redondeo cuando corresponda. Validar escala y límites en DTO y BD.

**Criterio de cierre:** Casos de medio centavo, varias líneas, descuento y exoneración deben mantener suma de componentes=total y débito=crédito a la precisión monetaria.

### FIN-06 · P1 · Notas fiscales editables dejan hash y asientos obsoletos

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:75](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:75)

**Evidencia y alcance:** T11: una nota emitida se edita de 100 a 25; FiscalHash permanece igual y su asiento sigue por 100. El bloqueo de edición solo considera Factura. T16 acepta creación directa de NotaCredito sin documento original ni motivo.

**Impacto:** Documento, snapshot y mayor contable dejan de describir la misma operación. Se puede eludir el flujo específico de notas.

**Corrección propuesta:** Inmutabilidad de todos los documentos emitidos; corregir mediante documento compensatorio y autorización apropiada. DTO y rutas por tipo con origen obligatorio y enum validado.

**Criterio de cierre:** PUT de cualquier documento emitido es rechazado; no existen notas huérfanas ni tipos numéricos fuera del enum.

### FIN-07 · P1 · Los libros suman documentos sin aplicar su naturaleza ni estado

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:67](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:67); [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:200](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:200)

**Evidencia y alcance:** Código y T10: agregados sin filtro de tipo/estado ni signo para notas; una factura anulada permanece en la selección y su nota entra con signo positivo. El ejemplo T10 no demuestra duplicación del ISV porque FIN-03 dejó el impuesto de la nota en cero.

**Impacto:** Ingresos e impuestos pueden incluir documentos anulados, notas y otros tipos como ventas normales. La UI llama a estas salidas oficiales sin una conciliación demostrada.

**Corrección propuesta:** Definir libro de eventos fiscales por tipo y signo, con trazabilidad al original; conciliar contra mayor y documentos, y validar formato con el responsable contable antes de usarlo para declarar.

**Criterio de cierre:** Mes con factura, nota parcial, nota total y documento no fiscal: libro, resumen y mayor coinciden y no incluyen ventas inexistentes.

### FIN-08 · P1 · Pagar compras no registra la salida de dinero

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/PurchaseInvoicesController.cs:95](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/PurchaseInvoicesController.cs:95); [backend/src/hotel-erp.Api/Services/AccountingService.cs:102](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AccountingService.cs:102)

**Evidencia y alcance:** T18: PUT /purchase-invoices/{id}/pay cambia estado; permanece un único asiento de compra. Toda compra usa gasto 5109 y crédito a proveedores, sin modelo de pago, vencimiento o clasificación de activo/inventario.

**Impacto:** Una factura pagada sigue como deuda contable y no reduce caja/banco. Compras de existencias o activos terminan en gastos genéricos.

**Corrección propuesta:** Registrar pagos aplicados a documentos, con asiento, caja/banco y referencia. Permitir clasificación contable de líneas y tratar impuestos recuperables según política validada.

**Criterio de cierre:** Compra 115 a crédito y pago 115 dejan proveedor 0 y banco -115; probar abonos, reverso y compra de activo/inventario.

### FIN-09 · P1 · Compras duplicadas y borrado con saldos contradictorios

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/PurchaseInvoicesController.cs:50](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/PurchaseInvoicesController.cs:50); [backend/src/hotel-erp.Api/Services/AccountingService.cs:151](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Services/AccountingService.cs:151); [backend/src/hotel-erp.Api/Database/Repositories/AccountingRepository.cs:79](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Repositories/AccountingRepository.cs:79)

**Evidencia y alcance:** T19 registra dos compras del mismo proveedor con igual número. T20 borra una y el catálogo muestra proveedor 230 mientras el balance de comprobación muestra 115: se marca eliminado el asiento por una ruta que no elimina sus líneas y los agregados aplican filtros distintos.

**Impacto:** Duplica gastos/deudas y ofrece saldos diferentes para una misma cuenta.

**Corrección propuesta:** Clave de documento de proveedor definida con el contador e índice único; reemplazar borrado de documentos contabilizados por reversos. Unificar filtros de cabecera/líneas en todos los agregados.

**Criterio de cierre:** Duplicado concurrente rechazado; todos los informes concilian antes y después de un reverso.

### FIN-10 · P1 · Caja y arqueos no representan cobros reales

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/CashRegistersController.cs:66](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/CashRegistersController.cs:66); [frontend/src/pages/reservations/CheckOutPage.tsx:210](/home/vm/Projects/erp-hotel/frontend/src/pages/reservations/CheckOutPage.tsx:210)

**Evidencia y alcance:** T22: apertura 1000, expectedAmount1 y countedAmount1 enviados por cliente cierran con diferencia0. T23: ventas no generan movimientos; el controller solo ofrece apertura/cierre. Checkout intenta POST /cash-registers/{id}/movements, ruta inexistente, y silencia el error.

**Impacto:** El arqueo puede ocultar faltantes y no permite conciliar ventas, devoluciones, anticipos y pagos. No hay turno financiero consistente.

**Corrección propuesta:** El servidor calcula saldo esperado desde movimientos inmutables por turno; vincular cada cobro/pago/reembolso y exigir motivo/aprobación para diferencias.

**Criterio de cierre:** Apertura 1000+venta 115-devolución 15 =>esperado 1100; el cliente no puede alterarlo. Caja cerrada rechaza movimientos y doble cierre.

### FIN-11 · P1 · Cuentas históricas y cierre contable carecen de protección efectiva

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:101](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:101); [backend/src/hotel-erp.Api/Controllers/AccountingController.cs:81](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/AccountingController.cs:81); [frontend/src/pages/accounting/JournalEntriesPage.tsx:194](/home/vm/Projects/erp-hotel/frontend/src/pages/accounting/JournalEntriesPage.tsx:194)

**Evidencia y alcance:** Los reportes excluyen cuentas inactivas aunque tengan historia; el tipo de cuenta puede editarse y cambiar signos retroactivamente. La interfaz dice que meses anteriores están cerrados por comparar con el mes actual; no existe entidad ni autorización de períodos cerrados en servidor.

**Impacto:** Desaparecen saldos y se puede modificar un período supuestamente cerrado llamando a la API. El primer día local también puede rechazarse por mezclar fecha UTC y medianoche local.

**Corrección propuesta:** Separar cuentas habilitadas para nuevos asientos de cuentas incluidas en informes. Bloquear cambios estructurales con historia; períodos explícitos, cierre/reapertura autorizados y auditados.

**Criterio de cierre:** Desactivar cuenta conserva todos sus saldos; API rechaza asientos en período cerrado, permite el primer día de un período abierto y conserva trazabilidad de reapertura.

### FIN-12 · P2 · Catálogo y estados financieros necesitan reglas de consolidación explícitas

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/AccountingController.cs:31](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/AccountingController.cs:31); [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:142](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:142)

**Evidencia y alcance:** Saldos de cuentas padre se calculan por movimientos propios, no consolidan hijos. Balance general acumula ingresos históricos y usa código 3102 para resultado; el catálogo inicial define otra cuenta para utilidad. No se identificó cierre anual que delimite resultado del ejercicio.

**Impacto:** La presentación jerárquica induce a interpretar subtotales que no existen; el resultado acumulado puede confundirse con el del período.

**Corrección propuesta:** Definir cuentas de movimiento y agrupación, roll-up sin doble conteo, resultados del ejercicio/acumulados y saldos de apertura aprobados por contabilidad.

**Criterio de cierre:** Plan de cuentas con dos niveles y dos ejercicios: conciliación exacta entre mayores, subtotales, resultado y patrimonio.

### FIN-13 · P1 · La API permite facturas con líneas e importes negativos

**Ubicación:** [backend/src/hotel-erp.Api/Dtos/common/InvoiceDtos.cs:107](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Dtos/common/InvoiceDtos.cs:107); [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:86](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:86)

**Evidencia y alcance:** T12: factura normal con cantidad 1 y precio -100 obtiene 201. El DTO de líneas usado para entrada carece de validaciones monetarias equivalentes a otros módulos; balancear un asiento con importes negativos no valida su significado.

**Impacto:** Puede reducir caja/ingresos mediante una factura ordinaria sin pasar por autorización, motivo y referencia de una nota de crédito.

**Corrección propuesta:** DTO de entrada específico con cantidad positiva, precio no negativo, descuento acotado, tasas permitidas y precisión definida; correcciones negativas mediante eventos/documentos autorizados.

**Criterio de cierre:** Factura con precio/cantidad negativos, descuento superior a 100 o tasa fuera de dominio devuelve 400 sin consumir correlativo ni guardar asiento.

### FIN-14 · P1 · El borrador DMR-1 muestra un cálculo de ISV sin modelo de retenciones

**Ubicación:** [frontend/src/pages/reports/ReportsPage.tsx:351](/home/vm/Projects/erp-hotel/frontend/src/pages/reports/ReportsPage.tsx:351); [backend/src/hotel-erp.Api/Controllers/ReportsController.cs:10](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReportsController.cs:10)

**Evidencia y alcance:** La pestaña DMR-1 reutiliza tax-summary (ventas, ISV de compras y tasa turística), con casillas 30/40/55/70 codificadas. No tiene detalle de retenciones por tercero/concepto. El SAR describe la DMR como información de retenciones efectuadas a terceros: [SAR, DMR](https://www.sar.gob.hn/dmr/). La discrepancia de alcance está confirmada; no se certifica aquí un formulario sustituto ni las obligaciones particulares del hotel.

**Impacto:** Un usuario puede confundir el resumen de ISV con la declaración de retenciones y trasladar valores a un trámite fiscal equivocado.

**Corrección propuesta:** Retirar la identificación DMR-1 hasta implementar y validar el formulario vigente aplicable. Separar ISV, compras y retenciones, con versión de normativa, período y conciliación por tercero; revisar junto al contador del hotel.

**Criterio de cierre:** El responsable contable compara una declaración completa con la guía oficial vigente; campos, bases y retenciones se corresponden con sus documentos, sin reutilizar un resumen de otro impuesto.

