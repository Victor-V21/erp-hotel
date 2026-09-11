# Lista 03 — operación hotelera, caja, compras e inventario

## S14–S15. Reservas, estancia y folio

- [x] Implementar máquina de estados y control de versión de reservas. Evidencia: [transiciones, versión optimista y motivo auditado](evidencia/registro.md#2026-09-08--lote-6-máquina-de-estados-de-reservas).
- [x] Separar disponibilidad por fechas del estado físico actual. Evidencia: [agenda sin estado físico `Reservada` y migración de datos](evidencia/registro.md#2026-09-08--lote-6-máquina-de-estados-de-reservas).
- [x] Aplicar intervalos `[entrada, salida)` y exclusión concurrente. Evidencia: [dos altas simultáneas producen una reserva y un conflicto](evidencia/registro.md#2026-09-08--lote-6-máquina-de-estados-de-reservas).
- [x] Validar capacidad, mantenimiento, fechas y cambios de habitación. Evidencia: [validación transaccional antes de confirmar cambios](evidencia/registro.md#2026-09-08--lote-6-máquina-de-estados-de-reservas).
- [x] Usar la reserva seleccionada en check-in y detener wizard ante errores. Evidencia: [recorrido real reserva → check-in → folio](evidencia/registro.md#2026-09-08--lote-6-máquina-de-estados-de-reservas).
- [x] Crear cargos con origen y asignaciones factura–folio. Evidencia: [líneas de folio enlazadas una sola vez a la factura de liquidación](evidencia/registro.md#2026-09-08--lote-5-check-in-y-liquidación-atómica-de-folio).
- [ ] Registrar/aplicar anticipos sin duplicar base ni ingreso.
- [x] Mostrar saldo completo y bloquear salida con cargos sin tratamiento. Evidencia: [checkout rechaza salida sin factura y liquida todas las líneas del folio](evidencia/registro.md#2026-09-08--lote-5-check-in-y-liquidación-atómica-de-folio).
- [ ] Aprobar HOT01–HOT10 y E2E01–E2E03.

Avance comprobado: reserva, confirmación, cancelación, check-in y checkout respetan transiciones válidas, locks y versión optimista; las cancelaciones conservan el cuarto y dejan motivo en auditoría. La interfaz lleva el ID seleccionado hasta check-in/check-out y el folio expone todas sus líneas. Los pagos parciales y mixtos ya se aplican a facturas emitidas; quedan pendientes anticipos, salida a crédito según política y las aprobaciones HOT/E2E.

## S16. Pagos y caja

- [x] Crear `Payment` y aplicaciones con estado, referencia, asiento y número propio. Evidencia: [auxiliar real y conciliación por origen](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Crear `Refund` y aplicaciones de devolución con autorización, medio y origen trazables. Evidencia: [auxiliar de reembolsos, límites y asientos por medio](evidencia/registro.md#2026-09-09--lote-9-reembolsos-y-secuencia-de-caja).
- [ ] Definir con el hotel y aplicar el circuito de aprobación segregada según monto, medio y origen.
- [x] Calcular cambio en servidor y soportar pago parcial/mixto explícito. Evidencia: [L100 efectivo + L19 transferencia sobre factura L119](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Exigir turno abierto para efectivo en la liquidación del folio. Evidencia: [la transacción rechaza caja cerrada y crea un único movimiento referenciado](evidencia/registro.md#2026-09-08--lote-5-check-in-y-liquidación-atómica-de-folio).
- [x] Aplicar pagos bajo bloqueo para impedir sobrecobro. Evidencia: [dos cobros concurrentes del último saldo producen un `201` y un `409`](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Calcular esperado de caja desde movimientos reales. Evidencia: [arqueo calculado y bloqueado en servidor](evidencia/registro.md#2026-09-08--lote-7-apertura-cierre-y-arqueo-de-caja).
- [x] Registrar liquidación de tarjeta, comisión, retención y depósito con aplicaciones a cobros. Evidencia: [lote del adquirente y asiento de compensación](evidencia/registro.md#2026-09-09--lote-10-liquidación-de-tarjeta).
- [x] Proteger cierre/reapertura y diferencias con motivo. Evidencia: [locks, idempotencia, auditoría y prueba concurrente](evidencia/registro.md#2026-09-08--lote-7-apertura-cierre-y-arqueo-de-caja).
- [x] Automatizar PAG01/PAG03, CON04 y TX03 en escenarios sintéticos de ingeniería. Evidencia: [integración PostgreSQL del auxiliar](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Automatizar un equivalente sintético de NC03 y PAG08 con reintento, concurrencia, caja y partida doble. Evidencia: [reembolso total y arqueo reconstruible](evidencia/registro.md#2026-09-09--lote-9-reembolsos-y-secuencia-de-caja).
- [x] Automatizar un equivalente sintético de PAG07 con bruto, depósito, comisión y retención. Evidencia: [L119 = L114 + L3 + L2](evidencia/registro.md#2026-09-09--lote-10-liquidación-de-tarjeta).
- [ ] Aprobar PAG01–PAG08 y CTA03.

Avance comprobado: factura, cobro, nota de crédito, reembolso y liquidación de tarjeta son eventos separados. El auxiliar admite abonos parciales, varios medios y reintentos idempotentes; la adquirencia concilia pagos seleccionados con depósito, comisión y retención sin tocar caja ni ingreso. Apertura, cobro, devolución y cierre comparten locks y una secuencia de caja reconstruible. Quedan pendientes anticipos, conciliación contra estados bancarios, reversos del adquirente, política de aprobación segregada y aprobación externa.

## S17–S18. Compras y retenciones

- [ ] Capturar documento completo de proveedor y validación fiscal.
- [ ] Añadir unicidad concurrente por proveedor/documento normalizado.
- [ ] Clasificar gasto, inventario, activo e ISV acreditable/no acreditable.
- [ ] Registrar abonos/pagos y notas del proveedor con asiento.
- [ ] Crear UI de compras/proveedores con permisos y errores.
- [ ] Implementar solo retenciones confirmadas por DEC-07.
- [ ] Guardar base, tasa, origen, constancia y liquidación contable.
- [ ] Implementar importación conciliada para auxiliares externos aplicables.
- [ ] Aprobar COM01–COM07 y RET01–RET05.

## S19. Inventario

- [ ] Sustituir edición directa por kardex de entradas/salidas/ajustes/reversos.
- [ ] Implementar método de valoración aprobado.
- [ ] Integrar recepción de compra, consumo de folio y costo de venta.
- [ ] Proteger stock bajo concurrencia e impedir negativos no autorizados.
- [ ] Exigir motivo/aprobación y asiento para ajustes físicos.
- [ ] Preservar costo histórico al cambiar precio comercial.
- [ ] Aprobar INV01–INV06 y CON05.

## Puerta G3

- [ ] Jornada reserva–salida sin duplicar cargos ni documentos.
- [ ] Cobros, caja, cartera y anticipos conciliados.
- [ ] Compras, proveedor, banco y retenciones conciliados.
- [ ] Kardex, stock, costo e inventario contable conciliados.
- [ ] Usuarios operativos y contador firman evidencia.
