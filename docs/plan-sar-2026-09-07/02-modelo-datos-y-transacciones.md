# Especificación de datos, operaciones y conciliación

**Diseño propuesto; no implementado.** Complementa S06–S23 del [plan principal](../auditoria-2026-09-06/08-plan-de-correccion.md). Los nombres nuevos son sugeridos; preservar las capacidades aunque cambien al implementar. Las reglas fiscales finales dependen de DEC-01–DEC-15.

## 1. Modelo objetivo y migración desde el código actual

| Agregado / ubicación propuesta | Cambio concreto | Invariantes y tratamiento histórico |
|---|---|---|
| `FiscalProfile`, `TaxRuleVersion` — nuevos en `B/Database/Entities/FiscalProfileEntities.cs` | Emisor, modalidad, vigencia, fuente/decisión, estado de aprobación; clasificación de conceptos e impuestos | Regla aprobada no se edita retroactivamente; nueva versión para cambios. Perfiles de prueba no pueden confundirse con reales |
| `FiscalAuthorization` — evolución de `InvoiceEntities.cs` | Emisor, establecimiento, punto, tipo, CAI, fecha inicial/límite, rango numérico, `LastIssued`, estado y archivo de autorización | Unicidad del número fiscal por emisor; controles de rango y vigencia. Tabla puente conserva `CAIId`/`DocumentAuthorizationId` anteriores |
| `Invoice` / documento fiscal | Estado fiscal separado de saldo; fecha fiscal, instante UTC, moneda, regla, snapshot, hash, versión del formato y referencias originales | Emitido inmutable. `PaymentStatus` derivado, no editable. Las referencias históricas no desaparecen por desactivar un catálogo |
| `InvoiceItem`, `InvoiceTaxComponent` — segundo nuevo | Cantidad, precio, descuento, base, impuesto/categoría/tasa/importe; referencia a línea original para notas | Componentes explican cada centavo. Ningún negativo de entrada simula devolución. Base 15 y 18 no se mezclan |
| `FiscalAdjustment` / referencias de nota | Documento origen, líneas, cantidad/base/impuestos acreditados, motivo y receptor/sustento | Crédito acumulado ≤ elegible; bloqueo para notas concurrentes. Anulación por error y crédito son operaciones distintas |
| `Payment`, `PaymentApplication`, `Refund` — nuevos | Pago real, medio, importe, referencia; aplicación a factura/anticipo; devolución y su motivo/origen | La suma aplicada no excede pago disponible ni saldo. El crédito fiscal no prueba devolución de efectivo |
| `CustomerAdvance` / depósito | Anticipo disponible, aplicaciones, devolución y vínculo con obligación fiscal cuando corresponda | Pasivo/otra clasificación aprobada; no descontarlo otra vez de la base del servicio |
| `CashSession`, `CashMovement` — evolución de `CashEntities.cs` | Turno abierto, apertura, movimientos originados en pagos/egresos, contado, esperado calculado, diferencia y aprobaciones | Sin movimiento económico no hay alteración de caja. No aceptar esperado/cambio del cliente |
| `CardSettlement` — nuevo | Pagos liquidados, bruto, comisión, retención sufrida y depósito bancario | Suma de componentes = bruto; recepción de tarjeta y depósito son eventos distintos |
| `FolioItem`, `InvoiceFolioAllocation` — segunda nueva | Cargo por noche/consumo con origen; asignación de cantidades/importes a líneas facturadas | No facturar un cargo dos veces. Reversión de cargo y nota tienen vínculos verificables |
| `Reservation` / estado físico de `Room` | Versiones, intervalos, estados permitidos, restricciones de cupo y ocupación independiente | Intervalos adyacentes permitidos; solapamientos activos prohibidos; cambio de cuarto mantiene historia |
| `PurchaseInvoice` / líneas | Proveedor/documento normalizado, clase de compra, producto/activo/cuenta, impuesto deducible/no deducible | Unicidad por proveedor/clave fiscal; no eliminar compras contabilizadas; validar crédito sin asumirlo por tener CAI |
| `Withholding`, `TaxFiling` — nuevos | Retención practicada/sufrida, concepto, base, tasa, soporte; preparación y presentación de declaración versionada | Ninguna declaración se marca presentada/pagada sin evidencia correspondiente; una rectificación conserva la previa |
| `InventoryMovement` — evolución de producto | Cantidad, costo, origen, clase, motivo, responsable y reverso | Stock y valoración derivables del kardex; prohibido alterar saldo sin movimiento. Origen único evita descuento duplicado |
| `JournalEntry` / líneas / `AccountingPeriod` | Origen único, estado, período, reverso, fecha, moneda funcional y reglas | Asiento contabilizado balanceado y no borrable; cierre efectivo en todas las rutas y bajo concurrencia |
| `OperationRequest` — nuevo | Clave de idempotencia, ámbito, hash de solicitud, estado y referencia al resultado | Índice único de ámbito+clave; autorización verificada al consultar/repetir; sin almacenar secretos del request |
| `OutboxJob` — nuevo | Tipo de trabajo, documento/recurso, estado, intentos, próximo intento, error seguro, versión | Persistido con la operación; procesamiento recuperable. Evitar afirmar impresión física a partir de un simple envío al spooler |
| `AuditEvent` — evolución | Secuencia, fecha UTC canónica, actor, acción, recurso, motivo, correlación, versión y hashes | Append-only para usuario de aplicación; cadena serializada y verificable; administrador del host conserva capacidad privilegiada |
| `BackupJob`, `BackupManifest` — nuevos o evolución | ID, estado, componentes, hashes, versión, localización protegida y resultado de restauración | Un nombre no identifica por sí solo un respaldo completo; nunca descargar «el último» de otro trabajo |

`B/` significa `backend/src/hotel-erp.Api/`. No es necesario crear una tabla por cada etiqueta si una implementación más sencilla conserva las invariantes; sí es obligatorio evitar usar una misma columna de estado para conceptos financieros diferentes.

### Precisión y fechas

Propuesta técnica: `numeric(18,2)` para importes monetarios finales en HNL; `numeric(18,6)` para precios/costos intermedios y `numeric(18,4)` para cantidades fraccionarias cuando el catálogo las permita. Tasas como decimal fraccionario con escala explícita, nunca `float/double`; porcentajes de UI se convierten una sola vez. Confirmar límites de negocio y no truncar datos preexistentes al migrar.

Guardar `FiscalDate` y vigencias como `date`. Eventos como `IssuedAtUtc` usan `timestamptz`. Las noches de estancia usan fecha civil del hotel; una estancia no cambia de día por convertirla como si fuera un instante UTC. Un mes contable no se consulta con `<= primer día del siguiente mes`.

La canonicalización del hash especifica campos/orden, representación decimal, UTF-8 y normalización, zona UTC y precisión compatible con lo persistido. Versionarla y probar ida/vuelta por PostgreSQL. No usar la serialización incidental de un objeto EF ni datos de navegación mutables como fuente del hash.

### Estados mínimos

- Documento: borrador → emitido; emitido → anulado solo por procedimiento admitido. Notas tienen su propio ciclo. «Con crédito parcial/total» puede derivarse de relaciones, conservando el original.
- Cobro: pendiente/parcial/liquidado/saldo a favor, derivado de aplicaciones, notas y devoluciones. Pago cancelado/revertido conserva referencia; no sobrescribirlo.
- Contabilidad: borrador → contabilizado → reversado mediante otro asiento. Período: abierto → cerrado; reapertura excepcional registrada.
- Trabajo de impresión: pendiente → en proceso → enviado/fallido/reintento; confirmación física solo si el dispositivo/procedimiento la proporciona.
- Declaración: preparada → revisada → presentada → pagada, con rectificaciones versionadas. Ninguna transición nace solo de descargar un archivo.

## 2. Operaciones y contratos propuestos

Las siguientes rutas son **propuestas**, no afirmaciones de que ya existan. Elegir el contrato final en S06, documentarlo y actualizar cliente/servidor a la vez; retirar rutas incompatibles al completar su transición.

| Operación | Contrato mínimo | Resultado y validación |
|---|---|---|
| Previsualizar venta | `POST /api/invoice-quotes`: conceptos, cantidades, tarifa pactada autorizada, comprador, beneficio y contexto | Totales del servidor, regla, versión/caducidad; no número fiscal, asiento ni reserva de correlativo |
| Emitir | `POST /api/invoices`: quote/contexto, autorización, líneas/orígenes y clave de idempotencia | ID, número, snapshot, importes y estado; revalidar vigencia, precio y permiso dentro del caso de uso |
| Crédito/débito | `POST /api/invoices/{id}/credit-notes` o `/debit-notes`: líneas, motivo, autorización y receptor requerido | Otro documento referenciado, asiento correcto y saldo; no editar factura original |
| Anular por error | `POST /api/invoices/{id}/void`: motivo, sustento, versión y autorización del operador | Evento fiscal/anulación/reversión según política; número preservado |
| Cobrar / devolver | `POST /api/payments`, `POST /api/payments/{id}/refunds` | Aplicaciones, caja/banco y asiento; rechazar excedentes, medio inválido y caja cerrada |
| Ingreso / salida | Comandos de reserva por ID con versión e idempotencia | Transición válida, folio y eventos necesarios; salida deja saldo explícitamente resuelto |
| Confirmar compra / pagar | Comandos de compra por ID, líneas y aplicaciones | Compra clasificada y contabilizada; pago crea evento financiero real |
| Ajustar inventario | Movimiento con origen/motivo/cantidad/costo y versión | Saldo/kardex coherentes y asiento cuando procede; sin PUT libre de stock |
| Cerrar período/caja | Comando con fecha/versión y evidencia del responsable | Revalidación en servidor y cierre concurrentemente seguro |
| Consultar operación | Consulta por clave autorizada o ID de operación | Resultado duradero después de pérdida de respuesta; no expone solicitudes de otros usuarios |

Usar 400 para contrato inválido, 401/403 para acceso, 404 para recurso no accesible según política y 409 para conflicto de versión/estado/clave/stock. Para regla de negocio se propone 422 con código estable; si se conserva 400, documentarlo uniformemente. Errores incluyen `code`, `message`, `fieldErrors` cuando aplica y `traceId`, sin detalles SQL internos.

## 3. Secuencia transaccional exigida

1. Autenticar/autorizar, normalizar solicitud y validar campos; capturar fecha mediante reloj de servidor.
2. Iniciar transacción y registrar/revisar clave de idempotencia en ámbito definido. Si ya confirmó, comprobar acceso y devolver el mismo resultado; si cambió el cuerpo, conflicto.
3. Bloquear recursos en orden documentado: período, reserva/folio, saldos de pago/crédito, stock y autorización, según el caso. Releer estado/versión tras el bloqueo; ninguna decisión crítica usa una entidad obsoleta precargada.
4. Resolver reglas vigentes y calcular. Si hay cambios frente a la cotización, devolver conflicto revisable sin emitir.
5. Asignar número de forma atómica. Persistir documento, líneas, componentes, asignaciones, pagos/movimientos y asientos aplicables; validar balance e invariantes.
6. Agregar auditoría y trabajo persistente de representación/impresión. Guardar referencia de resultado idempotente y confirmar todo.
7. Devolver el resultado. Un fallo antes del commit revierte el caso completo. Un fallo de red después del commit requiere recuperar el mismo resultado, no crear otro.
8. Procesar PDF/impresión/transferencia fuera de la transacción con intentos controlados. No «desemitir» por impresora desconectada.

El generador que actualiza `LastIssued` dentro de una transacción puede revertir una asignación nunca emitida. Si se elige una secuencia PostgreSQL no transaccional, documentar huecos y gestión de no utilizados; no prometer ausencia de huecos. En ningún diseño se reutilizan números de comprobantes efectivamente emitidos. Al recuperar un backup atrasado hay que reconciliar documentos posteriores antes de reabrir emisión.

## 4. Ejemplos de referencia que debe aprobar el contador

Los ejemplos E01–E08 son **oráculos técnicos sintéticos** en HNL. Fijan magnitudes para comprobar signos y conciliación; no sustituyen la clasificación fiscal de cada servicio real. Se omiten códigos numéricos de cuenta hasta DEC-09.

### E01. Venta a crédito y cobro independiente

Supuesto: servicio con base L100, ISV 15%, sin turismo ni descuento. Total L115.

| Evento | Debe | Haber |
|---|---|---|
| Emisión / reconocimiento según política | Cuentas por cobrar 115 | Ingreso 100 + ISV por pagar 15 |
| Cobro total en efectivo | Caja 115 | Cuentas por cobrar 115 |

Resultado: ingreso 100, pasivo ISV 15, caja 115, cliente 0. Una reimpresión no modifica ninguna cifra.

### E02. Crédito total de venta ya cobrada y devolución

Mismos datos de E01. La nota total revierte la venta/impuesto conforme a su tratamiento aprobado.

| Evento | Debe | Haber |
|---|---|---|
| Nota de crédito | Ingreso/devoluciones 100 + ISV por pagar 15 | Saldo a favor del cliente 115 |
| Reembolso efectuado | Saldo a favor del cliente 115 | Caja 115 |

Ingreso neto e ISV neto: cero. Caja neta: cero después del reembolso. Antes del reembolso existe deuda al cliente de 115, no una devolución ficticia. Si la factura estaba sin pagar, la nota disminuye cuentas por cobrar en vez de reconocer un pasivo duplicado.

### E03. Créditos parciales y límite acumulado

Factura E01. Crédito de base 40 + ISV 6 = 46; queda elegible base 60 + ISV 9 = 69. Otro crédito por 69 agota el saldo; un tercero por 0.01 debe rechazarse. Dos solicitudes simultáneas de crédito total tampoco pueden consumir el mismo saldo dos veces. La primera nota no marca anulada la factura completa.

### E04. Anticipo y aplicación sin duplicar ingreso

Ejemplo contable **condicionado a DEC-05**: un anticipo de L50 se reconoce inicialmente como pasivo sin devengo en este escenario sintético. Debe Caja 50 / Haber Anticipos de clientes 50. Al reconocer venta de E01: Debe Clientes 115 / Haber Ingreso 100 e ISV 15. Aplicación: Debe Anticipos 50 / Haber Clientes 50. Cobro final: Debe Caja 65 / Haber Clientes 65.

Resultado: ingreso 100, caja 115, anticipo 0, cliente 0. **Si el anticipo causa impuesto/emisión antes de prestar el servicio, implementar el evento fiscal correspondiente y su aplicación posterior, sin repetir base/impuesto.** Este ejemplo no autoriza omitir esa factura.

### E05. Documento mixto y tasa turística separada

Fixture de ingeniería con regla explícita de prueba: A base 100 al 15%; B base 100 al 18%; C exento 50; D exonerado 50. Sin turismo: subtotal neto 300, base15 100, base18 100, exento 50, exonerado 50, ISV15 15, ISV18 18, total 333. Cada clasificación es excluyente para el ISV de esa línea.

Fixture independiente, **sujeto a DEC-04 para uso real**: alojamiento neto 100, ISV sobre esa base 15 y turismo sobre esa misma base 4 → total 119. Añadir un servicio no sujeto a turismo debe dejar el turismo en 4. Si el perfil aprobado define otra composición de bases, sustituir este segundo fixture por el correspondiente; no convertir el divisor 1.19 en una regla universal.

### E06. Compra, abono y clasificación

Compra de inventario base 100 + ISV acreditable 15, total proveedor 115: Debe Inventario 100, Debe ISV crédito 15 / Haber Proveedor 115. Abono 40: Debe Proveedor 40 / Haber Banco 40; saldo 75. Pago final 75: saldo cero y banco disminuye 115 en total.

Una compra de gasto/activo usa su cuenta propia. Si el impuesto no es acreditable, su tratamiento se determina en DEC-09; no se mantiene automáticamente como crédito fiscal. El saldo del auxiliar debe coincidir con el mayor aunque se desactive el proveedor.

### E07. Liquidación de tarjeta y retención

Fixture contable sin fijar una tarifa legal: una venta ya registrada tiene cobro con tarjeta de 115. Se reciben 110 en banco, comisión documentada 3 y retención sufrida documentada 2. Debe Banco 110, Gasto/componente de comisión aprobado 3 y Retención a favor 2 / Haber Tarjetas por liquidar 115. El ingreso de la venta no baja a 110.

Para retención practicada al pagar proveedor, ejemplo con importe de retención validado `R`: Debe Proveedor `P` / Haber Banco `P-R` y Retenciones por pagar `R`. El entero posterior cancela la retención contra banco. Las tarifas y el tratamiento fiscal de comisiones se aprueban por separado.

### E08. Centavos, precios inclusivos y cierre

Con regla de prueba de redondeo por línea a dos decimales, base 0.03, ISV 15% y turismo 4% ambos redondean a 0.00; total 0.03. No admitir encabezado 0.04 con asiento de 0.03. Si la política aprobada redondea por documento, definir otro resultado esperado independiente y asignación determinista; en ambos casos debe cumplirse exactamente la igualdad.

Probar 0.005, varias líneas pequeñas, descuento repartido y crédito parcial seguido de total. El total final debe ser suma de bases netas más componentes redondeados; descuento ya restado de base no se resta nuevamente. No tolerar diferencia de 0.01 en el asiento por considerarla «pequeña».

## 5. Plan de migración del esquema por entregas

| Lote | Contenido | Validación antes de continuar |
|---|---|---|
| M-A | Añadir perfiles, versiones, fechas nuevas, puente de autorizaciones y columnas nullable de transición | Conteos iguales; cero pérdida de referencias; reporte de fecha origen y conversión por campo |
| M-B | Añadir componentes, snapshot, estados separados y asignaciones de folio | Comparar documentos originales; los históricos incompletos quedan identificados para revisión, no inventados |
| M-C | Pagos, anticipos, movimientos, cuentas, períodos, compras e inventario | Saldos iniciales y movimientos conciliados; no deducir pago solo de enum `Pagada` sin evidencia |
| M-D | Idempotencia, auditoría versionada, cola y trabajos de respaldo | Pruebas de rollback/reinicio; cadena antigua preservada y nueva verificable |
| M-E | Restricciones e índices definitivos; retirada de lectura/escritura obsoleta | Cero filas inválidas sin resolver; no quedan dos fuentes activas de verdad |

Cada lote requiere script revisado, ensayo en copia, estimación de bloqueo/espacio, respaldo, consulta de verificación y recuperación. No ejecutar `Down` destructivo en producción como sustituto de un plan de recuperación. Tras el corte, no mantener doble escritura entre modelos fiscal/contable viejo y nuevo sin conciliación formal.
