"""Índice, cobertura y anexos derivados del inventario y la evidencia conservada."""
from pathlib import Path
import json, csv, hashlib, collections, re
OUT=Path(__file__).resolve().parent.parent
ROOT=OUT.parents[1]
E=OUT/'evidencia'
rs=json.loads((OUT/'hallazgos.json').read_text())
inv=json.loads((E/'inventario-inicial.json').read_text())
counts=collections.Counter(r['prioridad'] for r in rs)
def write(name,text): (OUT/name).write_text(text)
def append(name,text):
    p=OUT/name;s=p.read_text();marker='\n<!-- ANEXO VERIFICADO -->\n';p.write_text(s.split(marker)[0]+marker+text)
groups={'contabilidad':'01-contabilidad-fiscal.md','operacion':'02-operacion-hotel-inventario.md','seguridad':'03-seguridad.md','infraestructura':'04-postgresql-docker-rendimiento.md','frontend':'05-frontend-experiencia.md','calidad':'06-calidad-codigo.md'}

write('README.md',f'''# Auditoría independiente de la versión actual del ERP hotelero

**Resultado: no recomiendo utilizar esta versión para facturación y control contable reales hasta cerrar los bloqueos e inconsistencias de integridad.** Se encontraron {len(rs)} hallazgos agrupados por causa: **{counts['P0']} P0, {counts['P1']} P1 y {counts['P2']} P2**. No equivalen a {len(rs)} vulnerabilidades explotadas: cada ficha identifica si hay reproducción o revisión estática.

Inicio: **6 de septiembre de 2026**, hora de Honduras. Cierre: **7 de septiembre de 2026**, tras reanudar la sesión. Se conserva el nombre de carpeta de inicio para mantener las evidencias enlazadas.

Versión inspeccionada: `/home/vm/Projects/erp-hotel`, commit **8e5bf6c603fb16519ef7652b0dbb610ac7e1c9de**, fechado **2026-08-23 12:09:02 -0600**. Es el árbol proporcionado en esta sesión; no se presupone que un despliegue externo ejecute ese mismo SHA. La comparación SHA256 al cierre verifica que los archivos preexistentes conservan su contenido inicial. Los diez cambios de `obj` ya presentes al comenzar se conservaron.

Se respetó el contexto de **un único equipo con contenedores, accesible por la LAN del hotel**. Las recomendaciones no exigen microservicios, Kubernetes ni infraestructura distribuida. La concurrencia de varias solicitudes, el fallo del disco, permisos internos y privacidad siguen siendo relevantes en ese escenario. **PostgreSQL ya está implementado** en esta versión mediante Npgsql y una migración inicial; Compose declara PostgreSQL16.

## Hallazgos determinantes

| Riesgo | Evidencia |
|---|---|
| Recepción obtiene privilegios de administrador renombrando roles | T42: nuevo rolAdmin y endpoint administrativo200; SEG-01 |
| Notas de crédito suman ingresos y no revierten la operación | T09: ingreso100 de factura +100 de nota; FIN-01 |
| Fallo de una operación deja datos guardados | T03 check-in con400 deja habitación/folio; T32 factura con500 queda sin asiento; FIN-02 |
| Caja, anticipos y compras pagadas no concilian con contabilidad | T18, T22, T23, T36 y T38; FIN-08/10, HOT-03/04 |
| Código actual no permite generar frontend desplegable | 7 diagnósticos de sintaxis en2 archivos; OPS-01 |
| Docker, proxy e impresión no están listos para el destino Linux/LAN | RutasCOPY y nginx verificadas por código; /print/printers500 enLinux; OPS-02/03/04 |
| La bitácora falla con registros intactos y no cubre todas las mutaciones | T30/T31 y reconstrucción exacta del hash; SEG-06/07 |

## Entregables

1. [Contabilidad y fiscalidad](01-contabilidad-fiscal.md): notas, impuestos, redondeo, caja, compras, mayor, cierres y DMR.
2. [Operación e inventario](02-operacion-hotel-inventario.md): reservas, disponibilidad, ingreso/salida, descuentos, stock e historia.
3. [Seguridad y privacidad](03-seguridad.md): autorización, sesiones, secretos, bitácora, red, dependencias y revisión de prácticas maliciosas.
4. [PostgreSQL, Docker y velocidad](04-postgresql-docker-rendimiento.md): mapeos, concurrencia, índices, impresión, respaldos y recuperación.
5. [Frontend y UX con impeccable](05-frontend-experiencia.md): puntuación, accesibilidad, contratos, errores, navegación y capturas.
6. [Calidad del código](06-calidad-codigo.md): build/lint, pruebas, arquitectura y entrega.
7. [Metodología y reproducciones](07-metodologia-pruebas.md): entorno, comandos, controles negativos y limitaciones.
8. [Plan priorizado](08-plan-de-correccion.md): dependencias, responsables sugeridos y puertas de aceptación.
9. [Cobertura por archivo](09-cobertura.md), [CSV de cobertura](cobertura-archivos.csv) y [registro de hallazgos JSON](hallazgos.json).

Las fichas contienen **archivo y línea, evidencia, impacto, corrección y prueba de aceptación**. Los resultados originales permanecen en `evidencia/`, y los scripts de laboratorio en `pruebas/`. No se cambió código funcional ni se aplicaron correcciones a datos reales. Las pruebas usaron datos sintéticos en otra base local, con puertos independientes.

## Prioridades

P0: impedir entrega/operación hasta corregir, por bloqueo de despliegue o riesgo directo de privilegios/integridad monetaria. P1: corregir antes de operar el módulo con datos reales. P2: mejora necesaria de fiabilidad, rendimiento o mantenibilidad con una solución temporal posible. Estas prioridades contextualizadas no son puntuaciones CVSS.

## Controles que sí funcionan

BCrypt para contraseñas; aleatoriedad criptográfica y hashes para refresh tokens; autorización anónima rechazada en endpoints probados; validaciones DTO en parte de los módulos; índice único que impidió duplicar correlativo; bloqueos de borrado de clientes/huéspedes con historia; migración limpia y restauración SQL sintética satisfactorias en PostgreSQL18.6. En UI hay componentes reutilizables, foco visible en controles base, tokens de tema y rutas con carga diferida.

No encontré evidencia de código escrito deliberadamente para robar información o dañar el equipo en las fuentes inspeccionadas. **Sí hay mecanismos explotables y prácticas inseguras**, especialmente permisos, secretos y respaldos. No se hizo ingeniería inversa de los binarios versionados ni análisis forense del equipo, por lo que no se certifica ausencia universal de malware.

## Alcance y límites esenciales

Se inventariaron **430 archivos versionados**: **186 no marcados como generados** y **244 generados/compilados**. Las fuentes, contratos, configuración y migración se revisaron por módulos y barridos; binarios, assets y logs tuvieron tratamiento específico documentado en la cobertura, no una supuesta revisión manual línea por línea de código compilado. La revisión de UX se realizó después de la revisión principal de backend.

Se ejecutaron43 casosAPI,4 comprobaciones de contrato frontend, una medición acotada de volumen, verificación independiente del hash y restauración de un dump sintético. No son43 pruebas que pasaron: incluyen errores reproducidos y controles que descartaron sospechas. No se probaron la impresora física, Docker completo, PostgreSQL16 ni datos reales del hotel. El frontend no compilable impidió una validaciónE2E completa del artefacto final; se inspeccionaron las páginas que Vite podía cargar sin modificar las fuentes.

La comprobación visual adicional de abrir un detalle de folio quedó bloqueada por la revisión automática de herramientas al agotarse un límite de uso durante la primera sesión. No se convirtió en prueba exitosa ni se sustituyó por un clic indirecto. El defecto de colecciones vacías sí está corroborado independientemente porAPI/SQL.
''')

write('08-plan-de-correccion.md','''# Plan de corrección y aceptación

Las etapas dependen de resultados, no de una estimación artificial de días. El sistema debe mantenerse en laboratorio hasta superar las puertas correspondientes. Si ya hubiera documentos reales, primero preservar respaldo completo y realizar conciliación; no eliminar ni recalcular masivamente historia sin revisión contable.

| Orden | Trabajo concreto | Hallazgos | Responsable sugerido | Puerta de salida |
|---|---|---|---|---|
| 1 | Hacer reproducible el build, corregir sintaxis, Docker y proxy; fijar un lockfile | OPS-01/02/03, QA-01/03 | Desarrollo y operación | Checkout limpio→imágenes→login por LAN y recarga de rutas |
| 2 | Cerrar roles/endpoints, secretos iniciales y descarga de dumps; separar puertos y credencialDB | SEG-01/02/03/04/08 | Backend y administrador | Matriz401/403 aprobada, bootstrap seguro y solo proxy expuesto |
| 3 | Estabilizar DTO, fecha fiscal y generación de correlativo | DB-01/02/03/04 | Backend | Mapeos válidos; líneas presentes; fechas/rangos y concurrencia correctos |
| 4 | Motor monetario único, inmutabilidad y transacciones idempotentes | FIN-01/02/03/04/05/06/13 | Backend con contador | Venta, nota, descuento y fallo inyectado concilian documento/asiento/pago |
| 5 | Aplicar pagos, anticipos, compras, caja y relación folio/documento | FIN-08/09/10, HOT-03/04/05 | Backend con recepción y contador | Jornada operativa completa con arqueo y mayor conciliados |
| 6 | Reservas, estados, stock, historia y períodos contables | HOT-01/02/06, INV-01/02, FIN-11/12 | Backend y responsables operativos | Carreras rechazadas; ninguna historia desaparece; cierre servidor efectivo |
| 7 | Libros, DMR, comprobantes y bitácora consistentes | FIN-07/14, OPS-05, SEG-06/07 | Contador y backend | Comparación con documentos de referencia y cadena íntegra tras restaurar |
| 8 | Reparar contratos y estados de UI, accesibilidad y navegación | UX-01 aUX-14 | Frontend y recepción | Recorrido por teclado, móvil, red caída y reintento sin duplicación |
| 9 | Afinar consultas/índices y resiliencia de respaldos | DB-05/06, OPS-06/07/08 | Backend y operación | Ensayo de volumen y recuperación del equipo dentro deRPO/RTO acordados |
| 10 | Ensayo de migración y liberación identificable | DB-07, QA-01/02/03, SEG-10 | Equipo y dueño del hotel | Conteos/totales/correlativos conciliados; release firmado y rollback probado |

## Jornada mínima de aceptación

1. Crear reserva futura sin bloquear físicamente el cuarto hoy; intentar solapamiento desde dos sesiones.
2. Registrar anticipo, ingreso con tarifa/descuento y factura correspondiente; imprimir en dispositivo real.
3. Agregar consumos de distintas tasas y un producto de inventario; comprobar stock, folio y contabilidad.
4. Emitir nota parcial por un cargo y un reembolso; verificar que la factura original no desaparezca.
5. Registrar compra a crédito, abono y pago final; conciliar proveedor e inventario/gastos.
6. Liquidar salida solo por saldo pendiente; cerrar caja con diferencia real y motivo autorizado.
7. Consultar libros y mayor del día, incluyendo límites de medianoche; cerrar período y rechazar cambios posteriores.
8. Cortar la conexión durante el cobro y reintentar la misma operación; debe existir un solo documento/pago.
9. Desactivar un usuario activo y demostrar que pierde acceso; verificar eventos de auditoría.
10. Recuperar en otro equipo y repetir lectura, impresión y verificación de saldos.

## Diseño adecuado para un único servidor

Mantener una aplicaciónASP.NET y PostgreSQL con un proxy de entrada. Separar dentro del código los casos de uso, no desplegar cada función como servicio. Una pequeña cola persistente para impresión/subida de respaldos puede vivir en la misma base/proceso con aislamiento de errores. Volúmenes persistentes paraBD y adjuntos; respaldo externo cifrado yUPS. Los índices y proyeccionesSQL aportarán más que añadir infraestructura antes de medir.

Las correcciones financieras requieren decidir con el contador el plan de cuentas, tratamiento de anticipos, exoneraciones, resultados acumulados y formatos fiscales aplicables. El informe identifica contradicciones técnicas demostradas; no sustituye esa definición del negocio.
''')

coverage=[]
for row in inv:
    path=row['archivo'];q=ROOT/path
    linked=[r['id'] for r in rs if any(x['archivo']==path for x in r['ubicaciones'])]
    if row['generado']:mode='Inventario/hash; artefacto generado, sin ingeniería inversa'
    elif path.endswith('.db'):mode='SQLite abierta solo lectura; esquema y conteos, sin extracción de datos personales'
    elif path.endswith('.log'):mode='Log histórico: clasificación y barrido diagnóstico; sin reproducir contenido sensible'
    elif path.endswith(('.png','.svg','.lscache')):mode='Asset/caché: inventario y referencias de uso; no auditoría binaria'
    elif '/Migrations/' in path:mode='Esquema contrastado con DbContext; migración ejecutada en laboratorioPG18'
    elif '/Controllers/' in path or '/Services/' in path or '/Repositories/' in path:mode='Revisión de módulo/flujo y barrido estático; ejecución selectiva según pruebas'
    elif '/Dtos/' in path or '/Entities/' in path:mode='Revisión de contratos/invariantes y barrido; contraste con persistencia'
    elif '/pages/' in path:mode='Revisión de lógica/contrato y barrido de JSX; visual selectiva, noE2E de todas las pantallas'
    elif path.startswith('frontend/src/'):mode='Revisión de rutas/componentes/estado y barrido estático'
    elif path.startswith('docs/'):mode='Documento previo leído como antecedente; no usado como prueba de corrección actual'
    elif 'lock' in path:mode='Inventario de versiones/instalación y contraste de dependencias; no revisión de código de terceros'
    else:mode='Revisión de configuración/entrega y barrido textual'
    sha=hashlib.sha256(q.read_bytes()).hexdigest() if q.exists() else None
    coverage.append({'archivo':path,'bytes':row['bytes'],'lineas':row.get('lineas'),'sha256_inicial':row['sha256'],'sha256_final':sha,'sin_cambio':sha==row['sha256'],'tratamiento':mode,'hallazgos':','.join(linked) or 'Sin hallazgo específico; no equivale a certificación'})
with (OUT/'cobertura-archivos.csv').open('w',newline='') as file:
    writer=csv.DictWriter(file,fieldnames=list(coverage[0]));writer.writeheader();writer.writerows(coverage)
(E/'integridad-final.json').write_text(json.dumps({'archivos':len(coverage),'sin_cambio':sum(x['sin_cambio'] for x in coverage),'cambios':[x['archivo'] for x in coverage if not x['sin_cambio']]},indent=2))
write('09-cobertura.md','''# Cobertura y trazabilidad

El inventario inicial contiene430 archivos versionados,186 no clasificados como generados y244 artefactos compilados/generados. [CSV completo](cobertura-archivos.csv) registra cada ruta, tamaño, líneas cuando procede, hashes inicial/final, tratamiento y hallazgos asociados.

“Sin hallazgo específico” significa que no se registró una causa separada en ese archivo; no es una garantía de corrección. Un archivo de DTO puede estar implicado en una ficha cuyo ancla principal está en el controller. Las referencias del registro señalan puntos útiles para corregir, no enumeran cada línea afectada.

Se revisaron controladores, servicios, repositorios, entidades, DTO, mapeos, arranque, migración, configuraciónDocker/nginx, rutas, estado, componentes y lógica de las26 páginas. El barrido automático recorre texto completo de archivos no generados adecuados; la lectura manual se concentró en lógica y contratos, con revisión selectiva del marcado repetido. [Barrido de patrones](evidencia/barrido-estatico.json) es un índice de revisión, no un escáner que certifique seguridad.

Assets e imágenes se inventariaron y se consideraron en las capturas de interfaz; no se atribuyeron fallos de producto a iconos de plantilla sin uso demostrado. SQLite se abrió en modo solo lectura para conocer esquema/conteos. Los logs históricos se trataron como potencialmente sensibles. Binarios y bundles previos no sustituyeron al código actual ni se descompilaron.

Las dependencias se evaluaron por manifiesto/lockfile, versiones instaladas y avisos publicados; no se auditó manualmente todo node_modules o código de NuGet. Cachés y binarios producidos por las pruebas quedan en subdirectorios de laboratorio excluidos del seguimiento; sus resultados útiles permanecen en evidencia.

## Fuentes actuales frente a auditorías previas

Se conservaron `docs/auditoria/`, `QA_FIX_PLAN.md` y `DEVELOPMENT_ROADMAP.md`. Esta auditoría no da por aplicada ninguna corrección de esos documentos. Reproduce sobre el árbolactual los errores de compilación y los93 avisoslint que también figuran en antecedentes: su coincidencia no significa que se haya usado la versión anterior. Se añaden pruebas sobre permisos, transacciones, correlativos, inventario, fechas, DTO yrestauración.

Se corrige especialmente cualquier lectura previa que considerase irrelevantes las advertencias de impresiónWindows: para los contenedoresLinux previstos son un impedimento comprobado. También se evita afirmar que PostgreSQL sea solo una migración futura: el código ya lo utiliza.

## Integridad del árbol

[Comparación al cierre](evidencia/integridad-final.json) frente a [inventario inicial](evidencia/inventario-inicial.json). Los14 archivosobj alterados por la compilación de auditoría se devolvieron a su hash inicial verificando que ese hash coincidía conHEAD; los10 cambios preexistentes se conservaron. No se modificaron fuentes funcionales, esquema del producto ni archivos de despliegue.
''')

def luminance(hex):
    rgb=[int(hex[i:i+2],16)/255 for i in (1,3,5)]
    values=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in rgb]
    return sum(c*w for c,w in zip(values,(.2126,.7152,.0722)))
contrasts=[{'fondo':h,'texto':'#ffffff','ratio':round(1.05/(luminance(h)+.05),3)} for h in ('#C69C4B','#22c55e','#eab308','#ef4444')]
(E/'contraste.json').write_text(json.dumps(contrasts,indent=2))
append('05-frontend-experiencia.md','''
## Evaluación impeccable

**Integridad de implementación: no aprobada.** Hay una identidad hotelera reconocible y componentes compartidos, pero las acciones financieras no tienen contratos coherentes y se muestran controles inexistentes. El bloqueo de compilación se registra una sola vez comoOPS-01 para evitar inflar el conteo del informe.

| Dimensión | Puntuación0–4 | Motivo |
|---|---:|---|
| Accesibilidad | 1 | Combobox contable inaccesible por teclado; labels/foco de modales y contraste incompletos |
| Rendimiento | 2 | Rutaslazy; faltan consultas paginadas, cancelación y carga por necesidad |
| Adaptación a pantalla | 1 | Breakpoints locales, pero shell fijo inutiliza gran parte del espacio estrecho |
| Temas | 2 | Tokens y modooscuro existentes; colores forzados se apartan del sistema |
| Integridad de implementación | 1 | Contratos fallidos, estados engañosos y controles sin implementación |
| **Total** | **7/20** | **Deficiente: necesita cambios importantes antes de pulido visual** |

Es una puntuación de auditoría contextual, no un resultadoLighthouse ni certificaciónWCAG. Las14 fichasUX se distribuyen en8P1 y6P2; los bloqueos de compilación y datos también afectanUI pero tienen sus propias fichas. No se añadieronP3 cosméticos que distraigan del trabajo esencial.

## Revisión del detector y falsos positivos

Se ejecutó una vez el detector oficial de la habilidad y se conservaron sus10 avisos en `evidencia/impeccable-detector.json`.

| Avisos | Decisión tras revisar código |
|---|---|
|2 sobreMontserrat “overused-font”|No se registra defecto por popularidad de una tipografía; la dependencia remota sí importa enLAN |
|1 bounce de404|Movimiento confirmado; se agrupa con política de movimiento reducido, sin exagerar su gravedad |
|6 gray-on-color enFolios/Autorizaciones|La herramienta cruza clases de ramas condicionales; cada rama usa gris/gris o verde/verde coherentes. Se descartan como evidencia de contrasteincorrecto |
|1 side-tab enCheckout|No basta para condenar el layout; la selección en un divsin teclado sí se revisa dentro de accesibilidad |

Los problemas de contraste del dorado/mapa son mediciones propias: [ratios](evidencia/contraste.json). No se infieren del detector. Se usa el umbral de texto normal4.5:1 y grande3:1 de[WCAG1.4.3](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html). La recomendación táctil44×44 es una meta ergonómica del playbook, no se afirma que todo botón28×28 viole automáticamenteWCAG2.2AA.

## Juicio de diseño y facilidad de operación

La marca dorada, encabezados de módulo, importes monetarios alineados y estados visibles son una base aprovechable. No hace falta rediseñar todo por estética. Primero debe quedar claro qué huésped/reserva se está operando, qué importe está pendiente, qué documento ya se emitió y si una acción quedó confirmada. La interfaz actual distribuye esa información entre reservas, folios, facturas y caja sin una liquidación central confiable.

Se recomienda un recorrido único de recepción: reserva→ingreso→consumos→saldo→salida, con identidad del huésped y estado financiero persistentes. Las pantallas administrativas pueden mantener mayor densidad, pero las operativas necesitan una acción principal y mensajes que indiquen resultado/reintento. Un resumen en cero no debe aparecer mientras no se conocen datos.

## Cobertura de las26 páginas

| Página/grupo | Revisión y resultado principal |
|---|---|
|Login|Captura y autenticación sintética; labels y botón decontraseña pendientes |
|Dashboard|Capturas escritorio/claro/oscuro/móvil; shell estrecho y promesa tiempo real |
|Habitaciones, tipos, mapa|Lógica y marcado; estados desalineados, guardado repetible, contraste y capacidad |
|Reservas, CheckIn, CheckOut|Contratos/flujo revisados; CheckIn no compila; identidad de reserva, liquidación y pagos divergentes |
|Huéspedes, clientes|Lógica y marcado; consultas por carácter, modal y validación fiscal |
|Facturas, edición, autorizaciones|Rutas/DTO/fechas y marcado; notas yPDF fallan, edición no concilia;4 contratos comprobados |
|Folios|Listado observado; líneasAPI vacías frente aBD y cargo400; apertura adicional de detalle visual bloqueada |
|Caja|Apertura/cierre y marcado; esperado calculado porcliente, no movimientosdeventa |
|Inventario|Lógica y marcado; descargas completas, edición directa de stock y carrera reproducida |
|Catálogo, asientos, comprobación|Sintaxis rota catálogo; selector conratón, cierre soloUI y estadosvacíos |
|Reportes|Fórmulas, consultas y marcado; borradorDMR equivocado,PDF inerte, filtros/resultados |
|Usuarios|Roles/validaciones y marcado; mínimos de contraseña y permisos |
|Auditoría|UI yAPI; inmutabilidad prometida no demostrada y cadena inválida |
|Descuentos|Reglas/formulario; tipos, límites y repetición deguardado |
|Respaldos|Descarga/errores yAPI; acceso excesivo y recuperación incompleta |
|Configuración|Preview/guardado; opciones sin enviar, logo y errores |
|404|Componente auxiliar; rebote sin alternativa de movimiento reducido |

## Capturas del código actual en Vite

Las imágenes provienen del frontend fuente, servido con un proxy de laboratorio haciaAPI sintética. No son prueba de buildproductivo exitoso. No hay datos reales de huéspedes.

![Login](evidencia/login-escritorio.png)

![Dashboard escritorio](evidencia/dashboard-escritorio.png)

![Dashboard oscuro](evidencia/dashboard-oscuro.png)

![Dashboard móvil](evidencia/dashboard-movil.png)

## Secuencia recomendada de trabajo con impeccable

1. **P0/P1 — `$impeccable harden`**: después de arreglar compilación/backend, contratos, errores, foco, teclado y reintentos.
2. **P1 — `$impeccable adapt`**: menú y contenido en390px, tabletas yzoom.
3. **P1 — `$impeccable colorize`**: contraste de botones y mapa sin perder identidad.
4. **P1/P2 — `$impeccable shape` y `$impeccable clarify`**: recorrido de reserva/liquidación y promesas de interfaz.
5. **P2 — `$impeccable optimize`**: carga por página, caché, cancelación, fuente local y límites de renderizado.
6. **P2 — `$impeccable audit`**: repetir evaluación y contratos después de corregir.
7. **P3 — `$impeccable polish`**: espaciado y consistencia final, una vez resueltos cálculos/acciones.

Estas acciones se pueden ejecutar una por una o en el orden acordado. En esta auditoría se documentaron; no se aplicaron cambios de diseño. Repetir la auditoría después de las correcciones permitirá verificar la mejora con la misma escala.
''')

append('03-seguridad.md','''
## Revisión de prácticas maliciosas

Se revisaron puntos de ejecución de procesos, SQL manual, URLs, configuración, hooks, autenticación y exportaciones. Los procesos externos identificados corresponden a `pg_dump` y a subida de respaldos mediante configuración/rclone. La solicitud externa de la UI esGoogleFonts; las APIs de negocio usan servidor relativo. Los hooks de editores ejecutan scriptsImpeccable de rutas personalesWindows cuando existen: son deuda de portabilidad, no prueba de un programa malicioso.

No se encontró `dangerouslySetInnerHTML`/`eval` usado para ejecutar datos del huésped en las fuentes revisadas. SQL manual de los locks usa interpolación parametrizada deEF. No se confirmó inyecciónSQL, ejecuciónremota, exfiltración oculta o un traversal que escriba fuera deUploads. Esas conclusiones son limitadas a las fuentes y patrones inspeccionados; no incluyen descompilar binarios ni revisar internamente todos los paquetes de terceros.

La escalaciónSEG-01 y descargaSEG-04 sí fueron ejecutadas con cuenta operativa en laboratorio. Son capacidad de abuso demostrada, sin inferir intención del autor. Una redLAN con varios usuarios necesita verificar permisos en cada solicitud; véase[OWASP Authorization](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html).

## Dependencias: alcance real del aviso

[Resumen de versiones](evidencia/dependencias-resumen.json), [npm audit](evidencia/npm-audit.json) y [NuGet](evidencia/nuget-vulnerabilidades.log) contienen resultados de consulta en esta sesión.

| Componente | Evaluación |
|---|---|
|AutoMapper12.0.1|Aviso de recursión profunda/DoS; no se envió una carga para derribar el proceso. Revisar profundidad y rutas de objetos controlables. Actualizar una versión mayor requiere revisar compatibilidad y condiciones de uso. [Aviso del mantenedor](https://github.com/LuckyPennySoftware/AutoMapper/security/advisories/GHSA-rvv3-g6hj-g44x) |
|Microsoft.OpenApi2.0.0|Aviso por leer documentosOpenAPI no confiables con referencias circulares; aquí se generaOpenAPI enDevelopment y no se identificó importación pública de especificaciones. Paquete afectado, explotaciónHTTP no demostrada. [Aviso deMicrosoft](https://github.com/microsoft/OpenAPI.NET/security/advisories/GHSA-v5pm-xwqc-g5wc) |
|Axios/router/Vite del locknpm|El scanner se refiere al locknpm; instalaciónpnpm usa versiones distintas. Avisos deSSR/RSC o adaptadorNode no se atribuyen automáticamente a estaSPA de navegador. Vite de desarrollo no debería publicarse como servidorproductivo |
|Babel, brace-expansion, browserslist, postcss, nanoid, form-data|Revisar alcance build/Node y entradas no confiables; no sumar cada advisory como vulnerabilidad remota delhotel |

LaSQLite versionada contiene1 usuario,7 registrosRefreshTokens y2AuditLogs; no contiene huéspedes ni facturas. Se conservaron solo conteos/esquema en el informe, no valores de contraseñas, sesiones o cambios del historial. Se debe evaluar y rotar material expuesto aun cuando la base sea antigua.
''')

append('04-postgresql-docker-rendimiento.md','''
## Evidencia de rendimiento y recuperación

[Medición completa](evidencia/rendimiento-local.json):3 lecturas antes y3 después, mismo servidor local.10 facturas→10.082bytes;5.010→4.998.975bytes. Los5.000 documentos extra eran réplicas sintéticas de cabecera sin líneas nuevas y se eliminaron del laboratorio al terminar la medición. No se usaron para conclusiones contables. [PlanSQL](evidencia/explain-facturas.txt): escaneo secuencial,24 filas útiles y4.986 descartadas. Unseqscan en tabla pequeña no prueba por sí solo lentitud grave; aquí acompaña al diseño de consulta no acotada y orienta la prueba de índices con volumenreal.

[RestauraciónSQL](evidencia/restauracion-local.json): exit0 en otra base localPG18,10 facturas/12líneas/2usuarios. Esto acredita restaurabilidad del dump sintético; no acredita respaldos automáticos, nube configurada, adjuntos ni recuperación del equipo. No se conectó una cuentaGoogleDrive del hotel.

El formato temporal explica el fallo dehash: [evidencia canónica](evidencia/hash-timestamp.json). PostgreSQL admite precisión de microsegundos, y `timestamp without time zone` no conserva una zona del instante; véase[tiposfecha PostgreSQL16](https://www.postgresql.org/docs/16/datatype-datetime.html). EF puede devolver una entidad ya seguida sin reemplazar sus valores por los recién consultados; esto es relevante alCAI precargado bajo lock: [tracking deEF](https://learn.microsoft.com/en-us/ef/core/querying/tracking).

La semántica delproxy conURI se contrastó con[documentaciónnginx](https://nginx.org/en/docs/http/ngx_http_proxy_module.html#proxy_pass). Para reservas concurrentes se propone estudiar[restricciones de exclusión](https://www.postgresql.org/docs/16/ddl-constraints.html) junto con rangos, según estados y política de cancelación reales. No se implementaron cambios de esquema durante la auditoría.
''')

print('Índice, cobertura y anexos escritos; integridad',sum(x['sin_cambio'] for x in coverage),'/',len(coverage))
