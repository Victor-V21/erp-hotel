# Lista 02 — núcleo fiscal, monetario y contable

## S07. Autorizaciones y correlativos

- [ ] Diseñar `FiscalAuthorization` y migración puente desde `CAI`/`DocumentAuthorization`.
- [ ] Estructurar establecimiento, punto, tipo, rango numérico, fechas y evidencia.
- [x] Implementar primer correlativo igual al rango inicial. Evidencia: [integración emite exactamente 001-001-01-00000001](evidencia/registro.md#2026-09-07--lote-3-perfil-fiscal-atomicidad-e-idempotencia).
- [x] Implementar incremento atómico con lectura fresca y límite de rango. Evidencia: [bloqueo transaccional PostgreSQL y prueba desde base nueva](evidencia/registro.md#2026-09-07--lote-3-perfil-fiscal-atomicidad-e-idempotencia).
- [ ] Añadir unicidad fiscal e impedir reinicio/reutilización desde UI.
- [ ] Alertar y bloquear por vencimiento, agotamiento, suspensión o tipo incorrecto.
- [ ] Registrar números no utilizados y su trámite.
- [ ] Aprobar FIS01–FIS07 y CON01–CON02.

Avance comprobado: la factura y su correlativo se crean dentro de la misma transacción; los índices fiscales únicos siguen activos. Quedan pendientes la migración puente completa, concurrencia masiva, números no utilizados y las aprobaciones de la sección.

## S08. Motor monetario

- [ ] Crear reglas versionadas por concepto y vigencia.
- [x] Separar bases 15%, 18%, exenta, exonerada y tasa turística. Evidencia: [cálculo y representación por concepto](evidencia/registro.md#2026-09-08--lote-4-motor-monetario-y-notas-fiscales).
- [x] Calcular líneas, descuentos, bases e impuestos exclusivamente en servidor. Evidencia: [motor compartido y pruebas](evidencia/registro.md#2026-09-08--lote-4-motor-monetario-y-notas-fiscales).
- [x] Rechazar cantidades/precios negativos, tasas no admitidas y descuentos fuera de política. Evidencia: [43 pruebas unitarias aprobadas](evidencia/registro.md#2026-09-08--lote-4-motor-monetario-y-notas-fiscales).
- [x] Fijar precisión y redondeo monetario por línea con AwayFromZero. Evidencia: [oráculo mixto 15 %, 18 % y exento](evidencia/registro.md#2026-09-08--lote-4-motor-monetario-y-notas-fiscales).
- [ ] Crear cotización versionada y recalcular al confirmar.
- [ ] Mantener tasa cero legítima; bloquear configuración ausente.
- [ ] Aprobar MON01–MON12 con oráculos independientes.

## S09–S11. Documentos, atomicidad y ajustes

- [ ] Separar estado fiscal del estado de cobro.
- [ ] Crear snapshot fiscal y hash canónico versionado.
- [x] Hacer inmutables facturas/notas emitidas. Evidencia: [la edición se rechaza y el cobro posterior no cambia snapshot, hash ni condición impresa](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [ ] Separar factura, proforma, recibo interno, nota y anulación.
- [ ] Implementar transacción integral e idempotencia por operación/usuario/request.
- [ ] Persistir cola posterior de representación e impresión.
- [x] Crear crédito por línea con límite acumulado bajo concurrencia. Evidencia: [FK a línea original, lock y respuesta 409 al exceder](evidencia/registro.md#2026-09-08--lote-4-motor-monetario-y-notas-fiscales).
- [x] Invertir correctamente ingreso/impuesto y crear saldo a favor si ya fue cobrada. Evidencia: [asiento de nota de crédito conciliado](evidencia/registro.md#2026-09-08--lote-4-motor-monetario-y-notas-fiscales).
- [x] Implementar nota de débito y anulación completa por nota de crédito con sustento. Evidencia: [integración HTTP y contable](evidencia/registro.md#2026-09-08--lote-4-motor-monetario-y-notas-fiscales).
- [ ] Aprobar FIS08–FIS12, TX01–TX08 y NC01–NC07.

Avance comprobado: factura, nota de crédito y nota de débito persisten documento, correlativo, asiento, auditoría, snapshot e idempotencia dentro de una transacción. La misma clave y cuerpo devuelve el mismo ID; la misma clave con otro cuerpo devuelve 409. El crédito copia precio, impuesto y descuento de la línea original, serializa el límite acumulado y reduce cuentas por cobrar antes de crear un saldo a favor. Pagos, aplicaciones y reembolsos ya tienen origen y asiento propios. Faltan compras, inventario, inyección de fallos y cola de impresión, por lo que G2 permanece abierta.

## S12–S13. Asientos y esquema

- [ ] Versionar mapeo evento–cuenta según DEC-09.
- [ ] Exigir partida doble, cuenta imputable, período abierto y origen único.
- [x] Separar devengo de factura, cobro y aplicación a cartera. Evidencia: [asientos distintos con origen `Invoice` y `Payment`](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Separar y conciliar la devolución al cliente contra saldo a favor y medio original. Evidencia: [Debe 2105 y Haber a caja/banco según origen](evidencia/registro.md#2026-09-09--lote-9-reembolsos-y-secuencia-de-caja).
- [x] Separar cobro con tarjeta de liquidación, comisión, retención y depósito bancario. Evidencia: [compensación completa de la cuenta 1104](evidencia/registro.md#2026-09-09--lote-10-liquidación-de-tarjeta).
- [ ] Separar y conciliar anticipo, compra y pago a proveedor.
- [ ] Corregir por reversión; impedir borrado de asientos contabilizados.
- [x] Evitar doble contabilización por índice único de origen activo. Evidencia: migración HardenInvoiceAdjustments y prueba de reintentos.
- [ ] Añadir migraciones aditivas, precisiones, FKs, checks e índices.
- [ ] Prevalidar y aislar historia inválida antes de constraints.
- [ ] Ensayar base nueva y actualización en PostgreSQL objetivo.
- [ ] Aprobar CTA01–CTA06, DAT04–DAT07, CON01–CON05 y M01–M03.

## Puerta G2

- [ ] Factura, nota, cobro y asiento concilian al centavo.
- [ ] Ningún fallo inyectado deja datos parciales.
- [ ] Reintentos y concurrencia no duplican documentos, pagos ni créditos.
- [ ] Documentos emitidos conservan número, snapshot e historia.
- [ ] Contador y QA aprueban ejemplos E01–E08 aplicables.
