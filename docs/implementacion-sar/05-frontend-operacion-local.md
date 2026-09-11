# Lista 05 — frontend, rendimiento y operación local

Aplicar el modo **Operate** de `impeccable`: rapidez, claridad, accesibilidad y prevención de errores para recepción/caja. Mantener la identidad visual existente durante el endurecimiento; cualquier rediseño completo requiere una dirección visual aprobada.

## S24. Contratos y flujos

- [ ] Crear capa API tipada y retirar rutas/métodos/cuerpos incompatibles.
- [ ] Corregir nota, cargo de folio, fechas, comprador y autorizaciones.
- [ ] Descargar PDF mediante petición autenticada y liberar URL temporal.
- [ ] Mantener reserva/huésped/formulario tras error.
- [x] Deshabilitar doble envío y usar idempotencia por intención en factura, notas, caja, pagos y reembolsos. Evidencia: [claves estables, reintentos y conflicto por cuerpo distinto](evidencia/registro.md#2026-09-09--lote-9-reembolsos-y-secuencia-de-caja).
- [ ] Extender idempotencia por intención a las mutaciones restantes.
- [x] Mostrar únicamente totales y saldos confirmados por API en checkout, caja y cartera. Evidencia: [recorrido visual de abono parcial](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Crear interfaz de abonos con efectivo/tarjeta/transferencia, caja abierta, referencia, cambio y auxiliar. Evidencia: [diálogo y actualización L119 → L19](evidencia/registro.md#2026-09-09--lote-8-auxiliar-de-pagos-y-cartera).
- [x] Crear interfaz de reembolso con crédito elegible, límite calculado, caja abierta o referencia externa, motivo y auxiliar. Evidencia: [recorrido visual del reembolso L119](evidencia/registro.md#2026-09-09--lote-9-reembolsos-y-secuencia-de-caja).
- [x] Crear interfaz de liquidación de tarjeta con selección de cobros, componentes, depósito calculado e historial. Evidencia: [recorrido visual L119 → depósito L114](evidencia/registro.md#2026-09-09--lote-10-liquidación-de-tarjeta).
- [ ] Completar UI de compras/proveedores/retenciones aplicables.
- [ ] Aprobar UI01–UI06 y E2E01–E2E05.

Avance comprobado: checkout, factura, notas, caja, pagos, reembolsos y liquidaciones de tarjeta envían una clave UUID estable por intención, la conservan ante respuestas inciertas y bloquean el doble envío. Las pantallas muestran saldos calculados por el servidor, exigen la evidencia propia de cada medio y mantienen auxiliares independientes del comprobante fiscal. Falta extender el patrón a las demás mutaciones y completar E2E01–E2E05.

## S25. Estados, accesibilidad y diseño

- [ ] Separar carga, vacío, sin resultados, sin permiso, error y éxito.
- [ ] Asociar labels, descripciones y errores; foco al primer error.
- [ ] Implementar teclado y foco de modales/combobox.
- [ ] Adaptar sidebar/tablas a 390 px, escritorio y zoom 200%.
- [ ] Corregir contraste de texto/acciones/estados y no depender solo de color.
- [ ] Alinear menú, permisos, breadcrumbs y paleta; retirar controles inertes.
- [ ] Alojar recursos esenciales localmente y respetar reducción de movimiento.
- [ ] Añadir ErrorBoundary y mensajes con acción de recuperación.
- [x] Ejecutar detector de `impeccable` una vez al terminar el lote UI. Evidencia: [detector sin hallazgos](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [x] Inspeccionar escritorio y móvil en una ronda; corregir en un lote y confirmar una vez. Evidencia: [flujo de contraseña temporal verificado en 1280 px y 390 px](evidencia/registro.md#2026-09-07--lote-1-build-autorizacion-y-sesiones).
- [ ] Aprobar UI07–UI13.

Avance comprobado: Configuración permite guardar, aprobar y retirar el perfil fiscal con vigencia, permisos, motivo y estados claros. El shell móvil usa un panel superpuesto y deja el contenido legible a 390 px; se verificaron apertura/cierre del menú y el flujo Borrador → Aprobado. Falta recorrer todos los módulos y zoom 200 %, por lo que UI10 y la puerta G5 no se dan por aprobados.

## S26–S27. Datos y velocidad

- [ ] Paginación/filtro/orden estables en servidor para listados grandes.
- [ ] Proyectar listas sin todos los hijos; detalle por ID.
- [ ] Debounce, cancelación y descarte de respuestas obsoletas.
- [ ] Timeout/retry seguro; mutaciones siguen idempotencia de S10.
- [ ] Eliminar N+1 y medir planes SQL.
- [ ] Añadir índices sustentados por filtros y medición.
- [ ] Medir p50/p95, tamaño, consultas, memoria y locks en hardware acordado.
- [ ] Implementar liveness/readiness y arranque que detecte esquema/configuración.
- [ ] Aprobar UI14–UI16, PERF01–PERF05 y OPS01–OPS02.

## S28. Respaldo y recuperación

- [ ] Incluir DB, adjuntos, documentos, configuración y manifiesto.
- [ ] Identificar trabajos por UUID y descargar por ID autorizado.
- [ ] Ejecutar procesos con argumentos seguros, timeout, stderr y cancelación.
- [ ] Serializar trabajos y aplicar retención sin borrar última copia válida.
- [ ] Conservar copia cifrada fuera del equipo y custodiar la clave aparte.
- [ ] Restaurar en equipo alterno y medir RPO/RTO.
- [ ] Documentar contingencia y reconciliación de correlativos.
- [ ] Aprobar OPS03–OPS08 y M04.

## Puerta G5

- [ ] Flujos críticos funcionan por teclado, a 390 px y zoom 200%.
- [ ] Fallos de red/servidor conservan datos y no duplican dinero.
- [ ] Objetivos de rendimiento acordados y medidos.
- [ ] Impresión Windows y recuperación en equipo alterno aprobadas.
