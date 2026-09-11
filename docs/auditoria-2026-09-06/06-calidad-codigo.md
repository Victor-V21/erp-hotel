# Calidad y mantenibilidad

### QA-01 · P1 · No hay una puerta automatizada que impida entregar código roto

**Ubicación:** [frontend/package.json:6](/home/vm/Projects/erp-hotel/frontend/package.json:6); [frontend/eslint.config.js:6](/home/vm/Projects/erp-hotel/frontend/eslint.config.js:6); [backend/src/hotel-erp.Api/hotel-erp.Api.csproj:4](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/hotel-erp.Api.csproj:4)

**Evidencia y alcance:** Build frontend falla y ESLint arroja 93 problemas:88 errores/5 warnings. No se encontró suite de pruebas de producto ni pipeline CI versionado. Backend compila con 10 warnings/0 errores usando paquetes existentes. Los scripts de esta auditoría son reproducciones, no una suite previa del producto.

**Impacto:** Regresiones sintácticas, contractuales y financieras llegan al despliegue; aprobar porque backend compila no valida ERP.

**Corrección propuesta:** CI con restore inmutable, build, lint y pruebas de invariantes: asientos, impuestos, idempotencia, permisos, concurrencia y contratos UI. Priorizar pruebas reales sobre snapshots que repitan implementación.

**Criterio de cierre:** PR con error de sintaxis, permiso abierto o asiento incorrecto falla CI; versión desplegada tiene SHA y resultados verificables.

### QA-02 · P2 · Lógica de negocio duplicada en controladores y navegador

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:113](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InvoicesController.cs:113); [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:276](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:276); [frontend/src/pages/reservations/CheckOutPage.tsx:155](/home/vm/Projects/erp-hotel/frontend/src/pages/reservations/CheckOutPage.tsx:155)

**Evidencia y alcance:** Cálculos y persistencia repartidos en controllers extensos, repositorios con SaveChanges y formulas TypeScript. DTO compartidos entre lectura/escritura permiten campos derivados y estados débiles. TaxService no concentra todos los caminos.

**Impacto:** Cada corrección puede dejar compras/notas/check-in/checkout divergentes; transacciones y testeo resultan difíciles.

**Corrección propuesta:** Casos de uso para emitir, acreditar, liquidar, ajustar stock y cerrar caja; dominio monetario común, DTO de entrada/salida distintos y controladores delgados. Refactorizar después de fijar pruebas de comportamiento.

**Criterio de cierre:** Una misma cotización produce resultados idénticos en todos los flujos y las pruebas ejercitan el servicio de negocio sin navegador.

### QA-03 · P2 · Falta trazabilidad de entrega y manual de operación local

**Ubicación:** [frontend/README.md:1](/home/vm/Projects/erp-hotel/frontend/README.md:1); [docs/QA_FIX_PLAN.md:1](/home/vm/Projects/erp-hotel/docs/QA_FIX_PLAN.md:1); [.codex/hooks.json:9](/home/vm/Projects/erp-hotel/.codex/hooks.json:9)

**Evidencia y alcance:** README frontend conserva contenido base; planes/auditorías previos no acreditan el build actual. Se versionan dos lockfiles, bundles/binarios generados y hooks a rutasWindows personales. No hay runbook completo de instalación, actualización, recuperación e impresora para el hotel.

**Impacto:** Otra persona puede ejecutar artefacto antiguo o perder datos al actualizar. Los hooks de una máquina no sustituyen CI ni son por sí mismos indicio de malware.

**Corrección propuesta:** Manifiesto de release con commit/imágenes/migraciones, un runbook probado y configuración portable. Vincular correcciones a evidencia nueva, mantener auditorías previas como historia.

**Criterio de cierre:** Técnico distinto instala y recupera usando solo documentación y secretos entregados de forma segura; puede identificar la versión en ejecución.

