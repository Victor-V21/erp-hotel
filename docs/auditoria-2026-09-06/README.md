# Auditoría independiente de la versión actual del ERP hotelero

**Resultado: no recomiendo utilizar esta versión para facturación y control contable reales hasta cerrar los bloqueos e inconsistencias de integridad.** Se encontraron 64 hallazgos agrupados por causa: **6 P0, 42 P1 y 16 P2**. No equivalen a 64 vulnerabilidades explotadas: cada ficha identifica si hay reproducción o revisión estática.

Inicio: **6 de septiembre de 2026**, hora de Honduras. Cierre: **7 de septiembre de 2026**, tras reanudar la sesión. Se conserva el nombre de carpeta de inicio para mantener las evidencias enlazadas.

Versión inspeccionada: `/home/vm/Projects/erp-hotel`, commit **8e5bf6c603fb16519ef7652b0dbb610ac7e1c9de**, fechado **2026-08-23 12:09:02 -0600**. Es el árbol proporcionado en esta sesión; no se presupone que un despliegue externo ejecute ese mismo SHA. La comparación SHA256 al cierre verifica que los archivos preexistentes conservan su contenido inicial. Los diez cambios de `obj` ya presentes al comenzar se conservaron.

Se respetó el contexto de **un único equipo con contenedores, accesible por la LAN del hotel**. Las recomendaciones no exigen microservicios, Kubernetes ni infraestructura distribuida. La concurrencia de varias solicitudes, el fallo del disco, permisos internos y privacidad siguen siendo relevantes en ese escenario. **PostgreSQL ya está implementado** en esta versión mediante Npgsql y una migración inicial; Compose declara PostgreSQL 16.

## Plan actualizado para la siguiente etapa

El 7 de septiembre de 2026 se amplió el [plan de corrección](08-plan-de-correccion.md) con 32 pasos de implementación, matriz normativa del SAR, modelo de datos/asientos, 186 casos de aceptación y trazabilidad de los 64 hallazgos. [Índice del plan y anexos](../plan-sar-2026-09-07/README.md).

El nuevo alcance toma Windows 11 como equipo destino y **difiere la implementación de contenerización** por indicación del usuario. Las correcciones descritas siguen pendientes: actualizar el plan no modifica los resultados históricos de esta auditoría ni acredita aprobación técnica o del SAR.

## Hallazgos determinantes

| Riesgo | Evidencia |
|---|---|
| Recepción obtiene privilegios de administrador renombrando roles | T42: nuevo rol Admin y endpoint administrativo 200; SEG-01 |
| Notas de crédito suman ingresos y no revierten la operación | T09: ingreso 100 de factura +100 de nota; FIN-01 |
| Fallo de una operación deja datos guardados | T03 check-in con 400 deja habitación/folio; T32 factura con 500 queda sin asiento; FIN-02 |
| Caja, anticipos y compras pagadas no concilian con contabilidad | T18, T22, T23, T36 y T38; FIN-08/10, HOT-03/04 |
| Código actual no permite generar frontend desplegable | 7 diagnósticos de sintaxis en 2 archivos; OPS-01 |
| Docker, proxy e impresión no están listos para el destino Linux/LAN | Rutas COPY y nginx verificadas por código; /print/printers 500 en Linux; OPS-02/03/04 |
| La bitácora falla con registros intactos y no cubre todas las mutaciones | T30/T31 y reconstrucción exacta del hash; SEG-06/07 |

[Índice completo de los 64 hallazgos](00-indice-hallazgos.md).

## Entregables

1. [Contabilidad y fiscalidad](01-contabilidad-fiscal.md): notas, impuestos, redondeo, caja, compras, mayor, cierres y DMR.
2. [Operación e inventario](02-operacion-hotel-inventario.md): reservas, disponibilidad, ingreso/salida, descuentos, stock e historia.
3. [Seguridad y privacidad](03-seguridad.md): autorización, sesiones, secretos, bitácora, red, dependencias y revisión de prácticas maliciosas.
4. [PostgreSQL, Docker y velocidad](04-postgresql-docker-rendimiento.md): mapeos, concurrencia, índices, impresión, respaldos y recuperación.
5. [Frontend y UX con impeccable](05-frontend-experiencia.md): puntuación, accesibilidad, contratos, errores, navegación y capturas.
6. [Calidad del código](06-calidad-codigo.md): build/lint, pruebas, arquitectura y entrega.
7. [Metodología y reproducciones](07-metodologia-pruebas.md): entorno, comandos, controles negativos y limitaciones.
8. [Plan detallado de adecuación al SAR](08-plan-de-correccion.md): pasos de código, dependencias, pruebas, responsables y puertas de aceptación; contenerización diferida.
9. [Cobertura por archivo](09-cobertura.md), [CSV de cobertura](cobertura-archivos.csv) y [registro de hallazgos JSON](hallazgos.json).

Las fichas contienen **archivo y línea, evidencia, impacto, corrección y prueba de aceptación**. Los resultados originales permanecen en `evidencia/`, y los scripts de laboratorio en `pruebas/`. No se cambió código funcional ni se aplicaron correcciones a datos reales. Las pruebas usaron datos sintéticos en otra base local, con puertos independientes.

## Prioridades

P0: impedir entrega/operación hasta corregir, por bloqueo de despliegue o riesgo directo de privilegios/integridad monetaria. P1: corregir antes de operar el módulo con datos reales. P2: mejora necesaria de fiabilidad, rendimiento o mantenibilidad con una solución temporal posible. Estas prioridades contextualizadas no son puntuaciones CVSS.

## Controles que sí funcionan

BCrypt para contraseñas; aleatoriedad criptográfica y hashes para refresh tokens; autorización anónima rechazada en endpoints probados; validaciones DTO en parte de los módulos; índice único que impidió duplicar correlativo; bloqueos de borrado de clientes/huéspedes con historia; migración limpia y restauración SQL sintética satisfactorias en PostgreSQL 18.6. En UI hay componentes reutilizables, foco visible en controles base, tokens de tema y rutas con carga diferida.

No encontré evidencia de código escrito deliberadamente para robar información o dañar el equipo en las fuentes inspeccionadas. **Sí hay mecanismos explotables y prácticas inseguras**, especialmente permisos, secretos y respaldos. No se hizo ingeniería inversa de los binarios versionados ni análisis forense del equipo, por lo que no se certifica ausencia universal de malware.

## Alcance y límites esenciales

Se inventariaron **430 archivos versionados**: **186 no marcados como generados** y **244 generados/compilados**. Las fuentes, contratos, configuración y migración se revisaron por módulos y barridos; binarios, assets y logs tuvieron tratamiento específico documentado en la cobertura, no una supuesta revisión manual línea por línea de código compilado. La revisión de UX se realizó después de la revisión principal de backend.

Se ejecutaron 43 casos API, 4 comprobaciones de contrato frontend, una medición acotada de volumen, verificación independiente del hash y restauración de un dump sintético. No son 43 pruebas que pasaron: incluyen errores reproducidos y controles que descartaron sospechas. No se probaron la impresora física, Docker completo, PostgreSQL 16 ni datos reales del hotel. El frontend no compilable impidió una validación E2E completa del artefacto final; se inspeccionaron las páginas que Vite podía cargar sin modificar las fuentes.

La comprobación visual adicional de abrir un detalle de folio quedó bloqueada por la revisión automática de herramientas al agotarse un límite de uso durante la primera sesión. No se convirtió en prueba exitosa ni se sustituyó por un clic indirecto. El defecto de colecciones vacías sí está corroborado independientemente por API/SQL.
