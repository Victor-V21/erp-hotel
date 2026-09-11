# Matriz normativa, perfil fiscal y decisiones pendientes

Fecha de consulta: **7 de septiembre de 2026**. [Volver al plan](../auditoria-2026-09-06/08-plan-de-correccion.md).

Este documento diferencia requisitos consultados, diseño propuesto y decisiones que necesitan documentación del hotel. No se tuvo acceso al RTN, obligaciones activas, autorizaciones reales, resoluciones de exoneración ni expediente de autoimpresor del establecimiento. No corresponde dar esos requisitos por aprobados.

## 1. Fuentes oficiales y uso dentro del plan

| ID | Fuente | Sustento y límite de la consulta |
|---|---|---|
| L01 | [SAR: texto consolidado del régimen de facturación, Acuerdo 481-2017 y reformas 609-2017, 725-2018 y 817-2018](https://www.sar.gob.hn/download/texto-consolidado-reglamento-del-regimen-de-facturacion-otros-documentos-fiscales-y-registro-fiscal-de-imprentas-contenido-en-el-acuerdo-481-2017-segun-acuerdos-609-2017-725-2018-y-817-2018/) | Arts. 9–11, 25–28, 38–43, 45–54: documentos, notas, impresión, custodia, registro y sistemas. La página de una consolidación no demuestra por sí sola ausencia de reformas posteriores; revalidar antes del uso real. |
| L02 | [SAR: Impuesto Sobre Ventas, leyes y ayudas](https://www.sar.gob.hn/impuesto-sobre-ventas-isv/) | Fuente para ley, reformas, declaración 201 y crédito fiscal. El índice consultado incluye ayuda 201 actualizada en agosto de 2026 y generalidades de créditos en septiembre de 2026; verificar la versión descargada, no inferir vigencia por el nombre del archivo. |
| L03 | [SAR: ayuda de tasa por servicios turísticos, código 259](https://www.sar.gob.hn/wp-content/uploads/2024/07/Ayuda-Declaracion-Jurada-Impuesto-Tasa-Por-Servicios-Turisticos-Codigo-259.pdf) | Generalidades, pp. 4–5: 4% sobre alojamiento diario; excepción para establecimientos de uso popular calificados por IHT; declaración/pago en los primeros diez días calendario del mes siguiente. La guía no resuelve aquí todos los casos de paquetes, descuentos o interacción de bases. |
| L04 | [SAR: Declaración Mensual de Compras](https://www.sar.gob.hn/declaracion-mensual-de-compras-dmc/) | DMC informa compras internas/importaciones gravadas y no gravadas. Utilizar ayuda/plantilla vigente y revisar aplicabilidad; el índice muestra actualizaciones de agosto de 2026. |
| L05 | [SAR: ISR y Acuerdo SAR-238-2024 sobre DJIMR](https://www.sar.gob.hn/isr/) | Retenciones a terceros tienen información y determinación propias. La denominación «DMR-1» de la UI actual no acredita correspondencia con el formulario aplicable. |
| L06 | [SAR: trámites de facturación](https://www.sar.gob.hn/tramitesfacturacion/) | Canales para autorizaciones, validación de documentos y notificación de uso de papel térmico. Archivar el trámite y resultado realmente utilizado por el hotel. |
| L07 | [SEDESOL: material oficial de descuentos de tercera y cuarta edad](https://www.sedesol.gob.hn/wp-content/uploads/2024/04/DESCUENTOS-3era-y-4ta-edad.pdf) | Referencia para beneficios hoteleros y diferencias por categoría/día. Completar elegibilidad, porcentajes, vigencia, base y compatibilidad consultando la ley/reforma aplicable antes de parametrizar. No es una regulación del SAR, pero afecta el precio y la base fiscal. |
| L08 | [SAR: ayuda 2026 del régimen simplificado de ISV, código 202](https://www.sar.gob.hn/wp-content/uploads/2026/03/DECLARACION-REGIMEN-SIMPLIFICADO-DEL-IMPUESTO-SOBRE-VENTA-CODIGO-202.pdf) | Remite a arts. 3, 6 y 12 de la ley de ISV para base, tasas 15%/18% y liquidación. No se presume que el hotel esté en régimen simplificado; la inscripción determina su declaración. |

Las fuentes normativas orientan las reglas. Los diseños de idempotencia, índices, colas, separación de estados, snapshots y pruebas son **propuestas de ingeniería para cumplirlas y corregir la auditoría**, no una afirmación de que el SAR exige una tecnología específica. Para facturación electrónica, verificar modalidad y obligaciones vigentes; no inventar un endpoint de envío al SAR ni equiparar CAI con CAEE. L01 distingue sistemas computarizados y autorización electrónica en arts. 53–54.

## 2. Requisitos que deben convertirse en controles verificables

| ID | Requisito / comprobación | Aplicación al código y evidencia | Pasos |
|---|---|---|---|
| NOR-01 | Identificar obligación de expedir comprobante y momento aplicable | Política por evento: anticipo, prestación, venta, penalización; incluir ventas exentas y supuestos gratuitos. Revisar art. 9 y reglas de causación de ISV. No usar el umbral de L50 para omitir facturas de un sistema computarizado autoimpresor. | S01, S08–S10, S15 |
| NOR-02 | Emisor, establecimiento, punto y modalidad corresponden al registro | Perfil fiscal versionado y expediente verificable. Registrar el sistema y declaración jurada cuando corresponda a autoimpresor. | S01, S07, S31 |
| NOR-03 | Factura con autorización válida y numeración del tipo correcto | Formato `NNN-NNN-NN-NNNNNNNN`; Factura 01, Nota de Crédito 06 y Nota de Débito 07 en los artículos consultados. Guardar CAI, rango y fecha límite del documento, no los ajustes actuales. | S07, S09, S23 |
| NOR-04 | Datos completos de formato y emisión | Lista de campos por tipo de comprobante contrastada con arts. 10–11/25–28. Plantilla de muestra revisada antes de autorizar emisión real. | S06, S09, S23 |
| NOR-05 | Crédito y débito documentados | Conservar origen fiscal, motivo, importes e impuestos; datos de receptor/firma cuando exigibles. Crédito contable y reintegro de efectivo son efectos separados. | S11, S12, S16 |
| NOR-06 | Impuesto separado por tarifa y naturaleza | Catálogo aprobado y componentes por línea; diferenciar ISV 15/18, exento, exonerado y turismo. | S01, S08 |
| NOR-07 | Beneficio fiscal sustentado | Orden/constancia/registro y alcance cuando correspondan; nunca asumir que cualquier comprador exonerado exonera también turismo. | S01, S08, S09 |
| NOR-08 | Tasa turística aplicable al establecimiento y concepto | Validar clasificación IHT; mantener auxiliar y declaración 259 separados de ISV. La tarifa 4% no justifica gravar lavandería o cualquier venta del hotel. | S01, S08, S21 |
| NOR-09 | Anulados y no utilizados conservados y tramitados | Separar anulación por error de créditos y de números no utilizados; art. 41 para anulados y art. 42 para aviso de no utilización. Registro de motivo, documentos/copias conservadas y trámite. | S07, S11, S28 |
| NOR-10 | Custodia e historia disponibles | Archivo ordenado, legible y recuperable; plazo y suspensiones de destrucción determinados con Código Tributario/leyes especiales (art. 43). No interpretar cinco años de papel térmico como permiso universal de borrado. | S09, S22, S28 |
| NOR-11 | Papel térmico con condiciones previas | Art. 38: certificado del proveedor presentado a la administración, legibilidad y garantía de al menos cinco años; revisar resúmenes de venta requeridos. Conservar original y copia fieles. | S01, S23, S31 |
| NOR-12 | Sistema computarizado integrado y controlado | Art. 53: integración al menos contable o inventarios, seguridad, auditoría, persistencia, disponibilidad histórica y generación de archivos de texto. Probar efectos reales; botones y clases sin integración no bastan. | S10, S12, S19, S21–S22 |
| NOR-13 | Crédito/costo de compras con validez y clasificación | Art. 39 y ley ISV: evidencia de comprobante, clasificación acreditable/no acreditable/prorrata cuando corresponda. | S17, S21 |
| NOR-14 | Declaraciones diferenciadas y conciliadas | Libro/auxiliar → casilla de formulario vigente → cuenta contable → revisión → presentación/pago. ISV, turismo, DMC y retenciones no comparten una fórmula genérica. | S18, S21 |
| NOR-15 | Actualización normativa controlada | Regla/formato con fuente, fecha, vigencia, responsable y pruebas. Cambio normativo no recalcula documentos previos. | S01, S08, S21, S31 |

La matriz es una lista de implementación, no una transcripción exhaustiva de la ley. En G0 el contador debe incorporar cualquier disposición adicional que afecte el perfil real y hacerla trazable a código, prueba o procedimiento externo.

## 3. Lista de campos para revisar el comprobante

Como contrato de diseño, crear una lista versionada por **tipo + modalidad + perfil**. No tratar todos los campos como obligatorios en todas las operaciones ni omitirlos por ser opcionales en otra modalidad.

| Grupo | Campos del modelo/representación propuestos | Validación |
|---|---|---|
| Emisor | RTN, razón/nombre registrado, nombre comercial, dirección matriz/establecimiento, teléfono/correo aplicable | Datos coinciden con registro y autorización; se congelan al emitir |
| Autorización | Modalidad, CAI o referencia autorizante pertinente, tipo, establecimiento, punto, rango inicial/final, límite, número completo | Correspondencia de tipo, fechas y rango; representación no usa un literal fijo |
| Comprador | Nombre/razón, RTN cuando corresponde, identidad para casos aplicables, dirección si requerida | Consumidor final no equivale automáticamente a exonerado; validar casos sin RTN con regla aprobada |
| Líneas | Concepto detallado, cantidad/unidad, precio, descuentos, base y clasificación | Suma reproducible; distinción entre precio incluido y precio antes de impuestos |
| Totales | Bases por tasa, exento/exonerado, ISV por tarifa, turismo separado, descuentos, total, moneda y letras cuando corresponda | Total coincide con snapshot y asiento; presentación sin recorte |
| Exoneración | Orden de compra exenta, constancia de exonerados y registro SAG si aplicable, titular, vigencia y soporte | Exigir solo campos aplicables según sustento, con alcance por impuesto y concepto |
| Notas | Tipo propio, CAI/número/fecha del origen, motivo, importes por tarifa, datos/firma de recepción exigibles | Vínculo inmutable; evidencia de entrega/recepción según procedimiento validado |
| Ejemplares | Destino original/copia, identificación de reimpresión y anulación cuando proceda | Original/copia fiscalmente fieles y archivados; ninguna reimpresión es otra venta |

## 4. Decisiones obligatorias de G0

**Estado inicial de todas las decisiones: PENDIENTE de validación del hotel.** Una decisión «no aplica» requiere sustento, responsable y fecha. No se necesitan estas respuestas para redactar el plan, pero sí para aprobar reglas reales.

| ID | Decisión concreta | Quién aporta y evidencia | Consecuencia si queda abierta |
|---|---|---|---|
| DEC-01 | Identidad fiscal, establecimientos, puntos, régimen y obligaciones activas | Administración/contador: constancias RTN y consulta vigente de obligaciones | No activar perfil real |
| DEC-02 | Modalidad de emisión, registro de sistema, CAI/tipos/rangos vigentes y eventual obligación electrónica | Administración/contador: autorizaciones, trámite, declaración jurada, resolución y especificación técnica aplicable | Emisión fiscal bloqueada; no asumir CAI o CAEE indistintamente |
| DEC-03 | Clasificación de cada servicio/producto, tasas y beneficios propios del hotel | Contador: matriz concepto–norma–impuesto–vigencia–cuenta | No emitir conceptos sin clasificar |
| DEC-04 | Sujeción/excepción IHT y composición de bases ISV/turismo en paquetes | Contador/IHT según corresponda: resolución y ejemplos de alojamiento y servicios separados | No usar factor general 1.19 como regla universal |
| DEC-05 | Anticipos, depósitos de garantía, devengo por noche, facturación, penalizaciones/no-show y reembolso | Contador + administración: casos de cobro antes/durante/después y estadía entre meses | No liberar check-in/out ni anticipos reales |
| DEC-06 | Descuentos comerciales/legales, elegibilidad, días/noches, acumulación y redondeo inclusivo | Contador + administración: normativa, documentos mínimos y tabla de casos firmados | No permitir porcentaje libre para reemplazar una política legal |
| DEC-07 | Retenciones practicadas/sufridas, tipos, tasas, exenciones, momento y formularios | Contador: perfil de obligaciones y documentos de respaldo | No declarar completo el flujo de compra/pago afectado |
| DEC-08 | Exoneraciones del comprador por concepto/impuesto, vigencia, límites y campos | Contador: expedientes y política de validación/renovación | No autorizar exoneración por un checkbox |
| DEC-09 | Plan de cuentas, ingresos/anticipos, valoración de inventario, activos, prorrata y ejercicio | Contador: catálogo y asientos de referencia | No aprobar contabilidad ni declaraciones |
| DEC-10 | Moneda funcional, cobros extranjeros, tipo de cambio, diferencias y medios de pago | Contador + caja: política; fuente oficial de cambio si se habilita moneda extranjera | Mantener operaciones exclusivamente en HNL y rechazar otras monedas mientras no se aprueben |
| DEC-11 | Anulaciones, notas sobre períodos cerrados, números no utilizados y contingencia | Contador + administración: procedimiento, responsables y canal de aviso | No habilitar acciones de excepción ni reutilizar números |
| DEC-12 | Plazos de conservación por clase, suspensión por fiscalización y eliminación autorizada | Contador + administración: tabla normativa y custodio | Mantener documentos; no purgar por antigüedad predeterminada |
| DEC-13 | Impresora, tamaño, original/copia, papel y expediente térmico | Administración + operación: equipo, prueba y certificado/trámite si corresponde | No dar impresión física por aprobada |
| DEC-14 | Versiones de PostgreSQL/.NET/Node, hardware, usuarios simultáneos, RPO/RTO, custodia de respaldos | Técnico + hotel: ficha del Windows 11 destino y restauración objetivo | Métricas del laboratorio no representan aprobación del destino |
| DEC-15 | Alcance de ISR, pagos a cuenta, nómina, activos y otras obligaciones no cubiertas actualmente | Contador: mapa obligación → módulo o auxiliar externo → responsable → evidencia | No afirmar que el ERP liquida todas las obligaciones del contribuyente |

## 5. Expediente de autorización y mantenimiento

Crear al implementar una carpeta protegida fuera del repositorio público con: perfil firmado; autorizaciones originales; registro del sistema/establecimiento/puntos; declaración jurada requerida; versiones de reglas; muestras de factura y notas; integración contable/inventario demostrada; instrucciones de exportación; protocolo de custodia; expediente térmico; actas técnicas/contables; trámite, acuse y resultado del SAR cuando exista. En `docs` conservar índice, hashes y evidencia redactada.

Responsable tributario revisa vigencia antes de liberar, al renovar CAI, al cambiar actividad/beneficio y antes de presentar cada período. Un cambio de ley genera tarea, regla con nueva vigencia y regresión; no modificación manual de facturas anteriores. No se han realizado trámites, declaraciones ni comunicaciones al SAR como parte de la elaboración de este plan.
