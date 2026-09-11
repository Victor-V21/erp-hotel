# Plan de implementación y adecuación al SAR

**Fecha:** 7 de septiembre de 2026. **Estado:** plan terminado; implementación, pruebas y aprobaciones pendientes.

El documento principal está en la carpeta de la auditoría, reemplazando su plan breve: [08 — Plan detallado de corrección](../auditoria-2026-09-06/08-plan-de-correccion.md).

El plan contiene **32 pasos S00–S31**, **2 trabajos de despliegue diferidos D01–D02**, **15 decisiones del hotel**, **186 casos de aceptación** y trazabilidad de los **64 hallazgos**. El objetivo inmediato es corregir y validar el software para el perfil fiscal del hotel, con pruebas locales y de impresión en Windows 11. Docker, Compose, nginx y la integración final de contenedores quedan para una etapa posterior expresamente identificada.

## Documentos y orden de lectura

1. [Plan principal](../auditoria-2026-09-06/08-plan-de-correccion.md): qué modificar, en qué archivos, en qué orden, quién revisa y qué evidencia permite avanzar.
2. [Matriz normativa y decisiones](01-matriz-normativa-y-decisiones.md): fuentes oficiales consultadas, campos de comprobantes, condiciones de aplicabilidad y expediente que debe completar el hotel.
3. [Modelo de datos y transacciones](02-modelo-datos-y-transacciones.md): entidades, migraciones, contratos propuestos, atomicidad e idempotencia, ocho ejemplos de conciliación y tratamiento de historia.
4. [Pruebas y aprobación](03-pruebas-y-aprobacion.md): catálogo con entrada, acción y resultado esperado, puertas G0–G6/GD y modelo de acta por versión.
5. [Trazabilidad legible](04-trazabilidad.md) y [CSV completo](04-trazabilidad.csv): cada hallazgo con prioridad, archivos originales, pasos, pruebas y estado pendiente/diferido.
6. [Catálogo JSON](catalogo-pruebas.json): casos estructurados para convertir posteriormente en pruebas y seguimiento; ninguno se marca ejecutado por haber sido definido.

## Cómo iniciar la implementación

Comenzar con S00: identificar versión/datos y preservar respaldo. Completar S01 con contador y administración; mientras tanto se puede corregir compilación y seguridad S02–S05 usando fixtures sintéticos. No fijar tasas generales, exoneraciones, formularios ni momento de facturación sin cerrar las decisiones aplicables.

Cada entrega debe vincular tarea, hallazgo, contrato/migración y evidencia de aceptación. El desarrollo termina cuando el comportamiento es verificable; la aprobación técnica, la validación contable del hotel y los trámites/autorizaciones del SAR son registros distintos. No se afirma certificación o conformidad absoluta de una versión todavía sin corregir.

## Qué se hizo en esta entrega documental

Se amplió el plan existente, se consultaron fuentes oficiales del SAR y SEDESOL, se diseñaron correcciones y pruebas, y se verificaron referencias y cobertura documental. No se aplicaron cambios al código funcional, migraciones de datos, declaraciones, trámites ni nuevas pruebas del sistema. Los archivos versionados fuera de `docs` se compararon por SHA256 con el estado al iniciar esta continuación; los cambios preexistentes de `obj` se conservaron.

[Validación documental](evidencia/validacion-plan.json). Los generadores de catálogo y trazabilidad están en `evidencia/`; no ejecutan pruebas de la aplicación ni modifican datos. Para corregir su contenido y conservar consistencia, actualizar el generador correspondiente y regenerar el Markdown/JSON/CSV. No usar los generadores históricos de la auditoría para sobrescribir este plan ampliado.
