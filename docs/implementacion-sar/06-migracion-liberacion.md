# Lista 06 — migración, aceptación y liberación

## S29. Datos y corte

- [ ] Identificar cada origen real/sintético y preservar original.
- [x] Crear el esquema aditivo de pagos sin inferir cobros desde estados históricos. Evidencia: [migración `AddPaymentLedger` y límite documentado](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Crear el esquema aditivo de reembolsos sin fabricar devoluciones históricas. Evidencia: [migración `AddRefundLedger`, ensayo descendente/ascendente y modelo alineado](evidencia/registro.md#2026-09-09--lote-9-reembolsos-y-secuencia-de-caja).
- [x] Crear el esquema aditivo de liquidaciones y aplicaciones de tarjeta sin inferir lotes históricos. Evidencia: [migración `AddCardSettlementLedger` y ensayo limpio/down/up](evidencia/registro.md#2026-09-09--lote-10-liquidación-de-tarjeta).
- [ ] Inventariar y conciliar facturas históricas marcadas como pagadas antes de importar aplicaciones reales.
- [ ] Crear mapeo de IDs, fechas, cuentas, autorizaciones, documentos y líneas.
- [ ] Importar historia sin recalcular ni inventar datos fiscales.
- [ ] Hacer la importación repetible y separar rechazos.
- [ ] Conciliar conteos, bases, impuestos, Debe/Haber, stock y correlativos.
- [ ] Ensayar congelamiento, respaldo, migración, verificación y recuperación.
- [ ] Diferenciar rollback previo de recuperación posterior a emisiones nuevas.
- [ ] Aprobar M01–M06.

## S30. Aceptación integral

- [ ] Ejecutar 186 casos aplicables y registrar aprobados/fallidos/bloqueados/no aplica.
- [ ] Ejecutar jornada completa con reserva, anticipo, cargos, factura, nota, compra, caja, kardex y cierre.
- [ ] Comparar cifras con oráculo independiente del contador.
- [ ] Hacer prueba operativa sin ayuda de desarrollo.
- [ ] Probar impresora, restauración, baja de usuario, período y correlativos.
- [ ] Entregar manuales por rol y capacitar responsables/suplentes.
- [ ] Aprobar E2E01–E2E06.

## S31. Aprobación y salida

- [ ] Cerrar todos los P0/P1 funcionales; documentar P2 solo con mitigación y vencimiento.
- [ ] Firmar actas técnica, contable/fiscal interna y operativa.
- [ ] Completar expediente de autorizaciones/trámites SAR aplicables.
- [ ] Revalidar normativa y perfil antes del primer documento real.
- [ ] Crear release reproducible, changelog y procedimiento de soporte.
- [ ] Conciliar diariamente las primeras cinco jornadas y el primer mes.
- [ ] Aprobar G6 sin presentar GD como completo.

## Contenedores diferidos

Estado: **DIFERIDA por instrucción del usuario**.

- [ ] D01: corregir Dockerfiles, frontend, proxy, `/api`, SPA, TLS, red, credenciales, volúmenes, healthchecks y migraciones.
- [ ] D02: decidir transporte de impresión desde el contenedor y probarlo en Windows 11.
- [ ] Repetir smoke completo desde los artefactos finales.
- [ ] Aprobar GD después de instalación limpia, acceso LAN, reinicios y recuperación.
