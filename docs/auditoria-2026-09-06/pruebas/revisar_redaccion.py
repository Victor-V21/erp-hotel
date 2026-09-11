"""Normaliza separación de palabras en los documentos, sin tocar fuentes del ERP."""
from pathlib import Path
import json,re
OUT=Path(__file__).resolve().parent.parent
phrases='contraste incorrecto|configuración persistida|con restore inmutable|operaciones financieras|pruebas de comportamiento|pruebas de invariantes|resultados anteriores|resultados de otro período|resultados de última consulta|selector ofrece otros|y controladores delgados|filtro de autorización|servidor productivo|build productivo|modo oscuro|por cliente|movimientos de venta|estados vacíos|de guardado|de contraseña|del hotel|ejecución remota|con ratón|volumen real|los resultados|su antigüedad|sin internet|de forma uniforme|no E2E|del dispositivo|por contenido|con fallback|sin recuperación|y contratos UI|un runbook|de uso|tiene onClick|maneja Escape|y zoom|con el|de entrada|todos los|de forma uniforme|árbol actual|por API|en SQL|a BD|al CAI|con RTN|con Z|en Linux|en LAN|en Development|de EF|de Microsoft|de Dashboard|de Uploads|y API|y PDF|y Enter|y UPS'
repls={''.join(x.split()):x for x in phrases.split('|') if len(x.split())>1}
repls.update({'filtrodeautorización':'filtro de autorización','deformauniforme':'de forma uniforme','contraseña6caracteres':'contraseña de 6 caracteres','a8enbackend':'a 8 en backend','tamaños11px':'tamaños de 11 px','valorinicial15':'valor inicial de 15','selectorofreceotros':'selector ofrece otros','registrosRefreshTokens':'registros RefreshTokens','oValidationProblemDetails':'o ValidationProblemDetails','porCreateInvoiceDTO':'por CreateInvoiceDTO','conrestoreinmutable':'con restore inmutable','repositoriosconSaveChanges':'repositorios con SaveChanges','ycontroladoresdelgados':'y controladores delgados','selecciónporEnter':'selección por Enter','tieneonClick':'tiene onClick','tipo100':'tipo 100','error500':'error 500','sinFiscalHash':'sin FiscalHash','noE2E':'no E2E','ycontratosUI':'y contratos UI','unrunbook':'un runbook','segúnpolítica':'según política','enproductoLAN':'en producto LAN','laBD':'la BD','LaSQLite':'La SQLite','unaSQLite':'una SQLite','estaSPA':'esta SPA','mismaAPI':'misma API','unaAPIready':'una API ready','carga porcontenido':'carga por contenido','loopbacklocal':'loopback local','losresultados':'los resultados','suantigüedad':'su antigüedad','configuraciónpersistida':'configuración persistida','conratón':'con ratón','sinratón':'sin ratón','todoslos':'todos los','cadaendpoint':'cada endpoint','se actualizanbajo':'se actualizan bajo','documentos de referencia':'documentos de referencia'})
for phrase in 'ajustar stock|al actualizar|archivos obj|autenticación local|avisos high|avisos lint|backend compila|balance cero|borrar logo|botón inactivo|catch fatal|compilación local|destinos localhost|dos lockfiles|e impresora|en controllers|encuentra facturas|en ejecución|en frontend|en proyecto|esperan response|estados aria|estados débiles|instalación pnpm|menos lecturas|mostrar contraseña|perder datos|permiso abierto|secretos entregados|timeout global|tipos fecha|un restore limpio|y cerrar caja|y chunk fallido|y reintento|y restauración|índice único|Build frontend|README frontend|documentación nginx|rol Admin|Rutas COPY|validación E2E|medición DOM|viewport 390|width 390|scrollWidth 575|reflow WCAG|zoom 200|estados Limpieza|formulas TypeScript|tiene SHA|valida ERP|con commit|con RTN|TaxpayerType Empresa|concurrencia CAI|BD local PG18|borrador DMR|Cargo 400|Descarga SQL|Build frontend|Fuente Google|Guía PDF|título FACTURA|imagen Linux|API sintética|OpenApi 2|AutoMapper 12|código 3102|Catálogo 230|Plan SQL|Reconstrucción SHA256|Registro inmutable|Rutas COPY|Git HEAD|PATCH parcial|dato incorrecto|readiness 503|Tasa 15|muestra 115|endpoint 200|administrativo 200|printers 500|User roles|leyenda completa|encuentra Facturación|puede generar|lista 200|modo oscuro|alias Recepcion|táctil 44|DateOnly válido|columna correcta|dato propio|token inicial|por línea|ambos temas|Ambos temas|ya facturado|data correcta|DocAuth válida|desde API|Probar 23|Servicio local|pago 115|tabla vacía'.split('|'):
    repls[''.join(phrase.split())]=phrase

def clean(s):
    # Proteger URLs y destinos locales de links, junto a identificadores de código.
    protected=[]
    def stash(m):protected.append(m.group());return f'@@PROTECTED{len(protected)-1}@@'
    s=re.sub(r'```[\s\S]*?```|`[^`\n]+`|https?://[^\s)]+|(?<=\]\()[^\n)]+',stash,s)
    for old,new in sorted(repls.items(),key=lambda x:-len(x[0])):
        s=re.sub(r'(?<![\w])'+re.escape(old)+r'(?![\w])',lambda _:new,s)
    s=re.sub(r'\b(de|del|al|en|con|sin|por|para|y|o|a|no|solo|desde|mediante|la|una|las|los)(?=[A-Z][A-Za-z0-9])',r'\1 ',s)
    s=re.sub(r'(?<=[a-záéíóúñ])(?=(?:API|SQL|BD|UI|DTO|WCAG|SHA256|CI|PDF|LAN|HTTP|RTN|DMR|CAI|SAR)\b)', ' ',s)
    keywords='arroja|compilación|produce|firma|clave|reflow|patrón|Patrón|casos|readiness|conserva|Contador|estado|primero|falla|ruta|runtime|PostgreSQL|Docker|Vite|cantidad|cupo|Cupo|capacidad|admite|recibe|obtiene|devuelve|reciben|responde|responden|publica|puerto|puertos|importe|base|Base|total|saldo|stock|Stock|factura|Factura|facturas|folio|Folio|ingreso|pagado|pagada|pago|pagar|precio|exento|Exento|gravado|Subtotal|subtotal|cambio|recibido|devuelve|esperado|Esperado|Apertura|apertura|Compra|compra|venta|descuento|anticipo|proveedor|mediana|nota|lista|listado|error|ninguna|otra|llamada|Con|con|de|del|a|en|y|por|entre|Las|Los|las|los|contra|contiene|arroja|ejecutaron|son|descarta|escanea|almacenó|coincide|incluye|filas|prueba|emitido|omitir|fin|inicio|rango|original|siempre|tasa|Página|página|día|mayor|entre|Control|corte|casillas|ejemplo|probar|Probar|Rechazar|Simular|Hora|hora|normal|grande|blanco|dorado|verde|amarillo|código|gasto|límite|devolución|extra|Estadía|estado|adultos|aceptado|cancelada|cerrada|inicial|final|termina|ambas|descarga|impresoras|Impresoras|resta|alto|casilla'
    s=re.sub(r'\b('+keywords+r')(?=\d)',r'\1 ',s)
    s=re.sub(r'(\d)(?=(?:bytes|ms|px|errores|warnings|warnings|líneas|usuarios|filas|registros|decimales|high|moderate|low|mil)\b)',r'\1 ',s)
    s=re.sub(r'(\d)(?=(?:P0|P1|P2|P3)\b)',r'\1 ',s)
    s=s.replace('Postgre SQL','PostgreSQL')
    s=re.sub(r'(?<=\d)(?=problemas\b)', ' ', s)
    s=re.sub(r'WCAG(?=\d)', 'WCAG ', s)
    s=re.sub(r',(?=[0-9A-Za-zÁÉÍÓÚáéíóúñÑ])', ', ', s)
    for i,p in enumerate(protected):s=s.replace(f'@@PROTECTED{i}@@',p)
    return s
for p in OUT.glob('*.md'):p.write_text(clean(p.read_text()))
p=OUT/'hallazgos.json';rs=json.loads(p.read_text())
for r in rs:
    for k in ('titulo','evidencia','impacto','recomendacion','aceptacion'):r[k]=clean(r[k])
p.write_text(json.dumps(rs,ensure_ascii=False,indent=2))
print('Revisión de separación de palabras aplicada a informes y fichas; fuentes intactas.')
