# Cobertura y trazabilidad

El inventario inicial contiene 430 archivos versionados, 186 no clasificados como generados y 244 artefactos compilados/generados. [CSV completo](cobertura-archivos.csv) registra cada ruta, tamaño, líneas cuando procede, hashes inicial/final, tratamiento y hallazgos asociados.

“Sin hallazgo específico” significa que no se registró una causa separada en ese archivo; no es una garantía de corrección. Un archivo de DTO puede estar implicado en una ficha cuyo ancla principal está en el controller. Las referencias del registro señalan puntos útiles para corregir, no enumeran cada línea afectada.

Se revisaron controladores, servicios, repositorios, entidades, DTO, mapeos, arranque, migración, configuraciónDocker/nginx, rutas, estado, componentes y lógica de las 26 páginas. El barrido automático recorre texto completo de archivos no generados adecuados; la lectura manual se concentró en lógica y contratos, con revisión selectiva del marcado repetido. [Barrido de patrones](evidencia/barrido-estatico.json) es un índice de revisión, no un escáner que certifique seguridad.

Assets e imágenes se inventariaron y se consideraron en las capturas de interfaz; no se atribuyeron fallos de producto a iconos de plantilla sin uso demostrado. SQLite se abrió en modo solo lectura para conocer esquema/conteos. Los logs históricos se trataron como potencialmente sensibles. Binarios y bundles previos no sustituyeron al código actual ni se descompilaron.

Las dependencias se evaluaron por manifiesto/lockfile, versiones instaladas y avisos publicados; no se auditó manualmente todo node_modules o código de NuGet. Cachés y binarios producidos por las pruebas quedan en subdirectorios de laboratorio excluidos del seguimiento; sus resultados útiles permanecen en evidencia.

## Fuentes actuales frente a auditorías previas

Se conservaron `docs/auditoria/`, `QA_FIX_PLAN.md` y `DEVELOPMENT_ROADMAP.md`. Esta auditoría no da por aplicada ninguna corrección de esos documentos. Reproduce sobre el árbol actual los errores de compilación y los 93 avisos lint que también figuran en antecedentes: su coincidencia no significa que se haya usado la versión anterior. Se añaden pruebas sobre permisos, transacciones, correlativos, inventario, fechas, DTO y restauración.

Se corrige especialmente cualquier lectura previa que considerase irrelevantes las advertencias de impresiónWindows: para los contenedoresLinux previstos son un impedimento comprobado. También se evita afirmar que PostgreSQL sea solo una migración futura: el código ya lo utiliza.

## Integridad del árbol

[Comparación al cierre](evidencia/integridad-final.json) frente a [inventario inicial](evidencia/inventario-inicial.json). Los 14 archivos obj alterados por la compilación de auditoría se devolvieron a su hash inicial verificando que ese hash coincidía con HEAD; los 10 cambios preexistentes se conservaron. No se modificaron fuentes funcionales, esquema del producto ni archivos de despliegue.
