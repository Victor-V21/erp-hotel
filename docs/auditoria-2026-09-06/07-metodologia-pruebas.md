# Metodología, pruebas y límites

## Identificación de versión

Se tomó el commit y estado de trabajo antes de empezar. El hash del commit es `8e5bf6c603fb16519ef7652b0dbb610ac7e1c9de`; los 430 archivos versionados tienen SHA256 inicial. Diez archivos obj ya estaban modificados. La compilación alteró14 archivos obj adicionales, restaurados únicamente cuando su contenido inicial coincidía exactamente con HEAD. [Estado inicial](evidencia/version.txt), [restauración de generados](evidencia/limpieza-generados.json) y [comparación final](evidencia/integridad-final.json).

La auditoría comenzó por backend, dominio, persistencia, autenticación, contabilidad, operación y despliegue. Después se aplicó la habilidad [impeccable](/home/vm/.agents/skills/impeccable/SKILL.md) disponible localmente, con su contexto/detector y revisión de UI. La instalación local de la habilidad es la fuente de instrucciones utilizada, no se descargó código nuevo de ese enlace para ejecutar la revisión.

## Entorno de laboratorio

| Elemento | Valor utilizado |
|---|---|
| Sistema | Linux del entorno de auditoría, distinto del equipo físico del hotel |
| .NET | SDK10.0.302, runtime 10.0.10, proyecto net10.0 |
| PostgreSQL |18.6 local; Compose del producto declara16-alpine |
| API | Código compilado del árbol actual, Production, `127.0.0.1:5089` |
| Base aislada |`hotel_audit`, puerto 55439, usuarioaudit_user, solo loopback |
| Restauración | Segunda base `hotel_audit_restore` en el mismo servidor aislado |
| Frontend | Fuentes actuales mediante Vite, puerto 5179, proxy API de laboratorio |
| Datos | Huéspedes, cuartos, facturas, roles y productos sintéticos |
| Respaldo automático/nube | Desactivado; se probó respaldo manual a carpeta del laboratorio |
| Docker | Docker 29.7.2 disponible; no se ejecutó la construcción/despliegue completo |

El laboratorio usa una claveJWT sintética y no consume la conexión de appsettings al hotel. No se accedió a un servidor productivo. PostgreSQL de prueba usa autenticación local permisiva, exclusivamente para estos datos sintéticos; no es una recomendación de configuración productiva. Los servicios de prueba se apagan al cerrar la auditoría.

Los tiempos observados incluyen código sin optimizar de la compilación local, cachés del proceso y hardware de este entorno. No se mide latencia de Wi-Fi del hotel ni rendimiento de la impresora. No se realizó carga de saturación ni prueba de agotamiento de disco.

## Compilación, lint y dependencias

| Comprobación | Resultado | Evidencia |
|---|---|---|
|`npm run build` en frontend|Falla:7 diagnósticos de sintaxis, 2 archivos|[log](evidencia/frontend-build.log)|
|`npm run lint` en frontend|Falla:93 problemas, 88 errores y 5 warnings|[log](evidencia/frontend-lint.log)|
|`dotnet build ... --no-restore -o .../backend-build`|Éxito:0 errores, 10 warnings|[log](evidencia/backend-build.log)|
|`npm audit --json`|10 paquetes del locknpm afectados|[JSON](evidencia/npm-audit.json)|
|Consulta NuGet de vulnerabilidades transitivas|2 paquetes con avisos altos|[log](evidencia/nuget-vulnerabilidades.log)|
|Detectorimpeccable|10 avisos; revisados, varios descartados|[JSON](evidencia/impeccable-detector.json)|

La compilación backend utilizó dependencias ya restauradas. No demuestra un restore limpio desde internet. La instalación local del frontend provenía de pnpm y difiere de package-locknpm; se guardó el contraste en [dependencias-resumen.json](evidencia/dependencias-resumen.json). Las consultas iniciales de dependencias encontraron restricciones de red; se repitieron con autorización y terminaron con los resultados conservados. No se hizo `audit fix` ni se actualizaron paquetes.

Los 93 problemas de ESLint no equivalen a 93 errores funcionales. Algunos corresponden a imports/variables sin uso, tiposany, dependencias dehooks o reglas deefectos. Una función declarada debajo de useEffect no implica por sí sola un error de ejecución: no se reportó esa inferencia como hecho.

Las advertenciasCA1416 de impresión sí tienen efecto demostrado en Linux. No fue posible medir bundle final, Lighthouse de producción o probarE2E todas las rutas porque la compilación frontend está bloqueada.

## Matriz de 43 reproducciones API

Los nombres originales de pruebas expresan hipótesis; la columna de interpretación corrige las que no se confirmaron. Resultados completos en [reproducciones-api.json](evidencia/reproducciones-api.json); scripts [primera ronda](pruebas/reproducir_api.py) y [segunda ronda](pruebas/reproducir_adicionales.py).

| Caso | Resultado relevante | Interpretación |
|---|---|---|
|T01|Login inicial 200|Credencial predecible de seed; SEG-02|
|T02|Anónimo/invoices401|Control positivo de protección anónima|
|T03|Check-in400 deja reserva/folio|Persistencia parcial; FIN-02|
|T04|Disponibilidad futura vacía|Estado físico bloquea fechas no solapadas; HOT-01|
|T05|Cancelar estadía200|Transición no protegida; HOT-02|
|T06|Confirmar cancelada 200|Transición no protegida; HOT-02|
|T07|Salida anterior a entrada204|Validación de edición incompleta; HOT-02|
|T08|Primer número termina 02 con inicio 01|Desfase de rango; DB-02|
|T09|Nota suma ingreso 100 e ISV0|Signo contable y cálculo de línea; FIN-01/03|
|T10|Resumen incluye original y nota|La nota tieneISV0: no prueba duplicación de ISV; lógica de filtros/signo se revisó en fuente; FIN-07|
|T11|PUTnota200 con hash y asiento anteriores|Inmutabilidad rota; FIN-06|
|T12|Precio -100 aceptado 201|Línea inválida; FIN-13|
|T13|ISV18 clasificado en ISV15|Desglose incorrecto; FIN-04|
|T14|Exento 100 clasificado gravado 100|Base incorrecta; FIN-04|
|T15|Ejemplo inicial de redondeo201|Sospecha no reproducida con esa entrada; sustituida por T32|
|T16|Nota sin origen/motivo201|Elusión de ruta específica; FIN-06|
|T17|Autorización insertada y respuesta500|Mapeo de fecha; DB-04|
|T18|Pagar compra 204, único asiento|No hay asiento de pago; FIN-08|
|T19|Compra duplicada201|Duplicidad sin restricción; FIN-09|
|T20|Catálogo 230 vs comprobación115|Filtros de borrado distintos; FIN-09|
|T21|Borrar proveedor oculta compras|Historia/maestro acoplados; HOT-06|
|T22|Esperado 1 frente a apertura 1000: diferencia0|Servidor confía en cliente; FIN-10|
|T23|Solo movimientos apertura/cierre|Ventas sin movimientos de caja; FIN-10|
|T24|Stock -10 sin movimiento|Elusión de kardex; INV-01|
|T25|Recepción crea cuenta/exporta/ve respaldos|Permisos insuficientes; SEG-03/04|
|T26|Token desactivado aún200|Revocación incompleta; SEG-05|
|T27|Impresoras 500 en Linux|Incompatibilidad Windows; OPS-04|
|T28|Concurrencia CAI:201 y 500|EF precargado y duplicidad rechazada; DB-01; índice único funciona|
|T29|Consultas por enum200|No se confirmó fallo de traducción de ToString con este proveedor|
|T30|Bitácora cubre login/facturas|Mutaciones críticas sin traza; SEG-07|
|T31|Cadena propia inválida, 0 verificados|Falso positivo de corrupción; SEG-06|
|T32|Base 0.03 con 15%+4%:500|Reproducción válida del redondeo; FIN-05|
|T33|Factura 0.04 sin asiento|Verificación SQL del efecto parcial; FIN-02|
|T34|Items API[] con 11 líneas en BD|Colecciones no mapeadas; DB-04|
|T35|Una autorización en BD, listado 500|Persistencia pese a error; DB-04|
|T36|Cupo 2/adultos 10/recibido 1/cambio 500|Check-in aceptado; HOT-03|
|T37|FacturaCheckIn pagada sin FiscalHash|Snapshot omitido en este camino; OPS-05|
|T38|Folio 1305 cerrado con factura 1190|Consumo pendiente ignorado; HOT-04|
|T39|NuevoCheckIn de reserva cerrada 500|Estado y folio no idempotentes; HOT-02|
|T40|Dos salidas7: ambas 200, stock 3|Actualización perdida, salida total 14; INV-01|
|T41|Recepción descargaSQL200|Exposición total de BD; SEG-04|
|T42|Recepción obtieneAdmin y endpoint 200|Escalación de privilegios; SEG-01|
|T43|/health200|Solo prueba respuesta de salud; ausencia de consultaDB verificada en código, no se cortó la BD; OPS-08|

## Pruebas adicionales

| Caso | Resultado | Evidencia |
|---|---|---|
|C01 carga de consumo como UI, sin FolioId|400|[contratos](evidencia/contratos-frontend.json)|
|C02 ruta de nota crédito usada por UI|405|[contratos](evidencia/contratos-frontend.json)|
|C03 TaxpayerType Empresa usado por UI|400|[contratos](evidencia/contratos-frontend.json)|
|C04 DateOnly con timestampISO usado por UI|400|[contratos](evidencia/contratos-frontend.json)|
|Volumen:10→5.010 facturas|Respuesta≈10KB→5MB y≈8–16 ms→397–562 ms|[medición](evidencia/rendimiento-local.json)|
|EXPLAIN de rango deun día|24 filas, 4.986 descartadas, seqscan|[plan](evidencia/explain-facturas.txt)|
|Reconstrucción SHA256|Hash original coincide con 7 decimales y Z, no con timestamp persistido|[hash](evidencia/hash-timestamp.json)|
|Restauración SQL en segunda BD|exit0;10 facturas, 12 líneas, 2 usuarios|[resultado](evidencia/restauracion-local.json)|
|Contraste sRGB|Blanco/dorado 2.543; blanco/verde 2.279; blanco/amarillo 1.918|[cálculo](evidencia/contraste.json)|

El ensayo de carga copió solo cabeceras para aislar el costo del listado; no es un conjunto de contabilidad válido. Se borraron esas réplicas al terminar. No se midieron p95/p99 confiables con tres muestras ni se extrapoló capacidad máxima del hotel.

## Repetición en un laboratorio nuevo

Los scripts mutan intencionalmente datos sintéticos para demostrar errores y fijan destinos localhost. No deben apuntarse a la base del hotel. Para repetir exactamente, usar un checkout/copia de laboratorio de la versión identificada, con .NET10, PostgreSQL y Node compatibles. Los comandos siguientes se ejecutan desde la raíz del proyecto y requieren puertos 55439/5089/5179 libres.

En un directorio de pruebas nuevo (el conservado de esta auditoría ya contiene datos), inicializar PostgreSQL:

```bash
initdb -D docs/auditoria-2026-09-06/pruebas/pgdata -U audit_user --auth=trust
pg_ctl -D docs/auditoria-2026-09-06/pruebas/pgdata -l docs/auditoria-2026-09-06/evidencia/postgres-server.log -o '-h 127.0.0.1 -p 55439 -k /tmp' -w start
dotnet build backend/src/hotel-erp.Api/hotel-erp.Api.csproj --no-restore -o docs/auditoria-2026-09-06/pruebas/backend-build
python docs/auditoria-2026-09-06/pruebas/iniciar_api.py
```

El último comando permanece sirviendo API. `--no-restore` supone paquetes restaurados previamente; para validación de release hacer también un restore limpio con el lock/gestor elegido. `iniciar_api.py` crea la BD y falla si ya existe: es una salvaguarda contra sobreescribir datos, no un script de reinicio. En otra terminal:

```bash
python docs/auditoria-2026-09-06/pruebas/reproducir_api.py
python docs/auditoria-2026-09-06/pruebas/reproducir_adicionales.py
python docs/auditoria-2026-09-06/pruebas/comprobar_contratos.py
python docs/auditoria-2026-09-06/pruebas/comprobaciones_finales.py
node frontend/node_modules/vite/bin/vite.js --config docs/auditoria-2026-09-06/pruebas/vite-audit.config.mjs
```

Las pruebas dependen de que la autorización sintética siga vigente y de la política de arranque de esta versión; si se repiten después de 2027, ajustar únicamente fechas sintéticas. No son pruebas estables de CI todavía: registran comportamiento observado y necesitan convertirse en aserciones de aceptación después de corregir. Guardar resultados de cada nueva ejecución en otra carpeta para no borrar la evidencia de esta auditoría.

La configuraciónVite del laboratorio permite cargar páginas no afectadas por sintaxis sin alterar los archivos del producto. El servidor advierte fallos del escaneo de dependencias y las rutas quebradas no pueden completarse. No se debe confundir poder abrirDashboard con tener una aplicación compilable.

Tras terminar: detener únicamente los procesos API/Vite del laboratorio y ejecutar `pg_ctl -D .../pruebas/pgdata -m fast -w stop`. Nunca matar servicios del hotel por nombre genérico.

## Fuera de verificación directa

No se probaron explotación destructiva de dependencias, carga ilimitada, fuerza bruta, escritura fuera de Uploads, fuga por red ajena, recuperación de datos reales ni legislación completa aplicable al hotel. Los fallos de DockerCOPY/proxy, cobertura de auditoría, cierres contables e integración se sostienen en código; sus criterios de cierre exigen nuevas pruebas después de implementar. La evaluación de malware no incluye ingeniería inversa de binarios ni análisis del sistema operativo.

Los formatos fiscales, tasas por producto, exoneraciones y políticas de cuentas deben validarse con el responsable contable y fuentes oficiales. Se consultó[DMR del SAR](https://www.sar.gob.hn/dmr/) para confirmar que el formulario de retenciones no corresponde al resumenISV mostrado. No se certificó un formulario sustituto ni se recomendó presentar los reportes actuales.

## Cierre del laboratorio

API, Vite y PostgreSQL de pruebas fueron detenidos. [Registro del cierre](evidencia/cierre-laboratorio.json). Los archivos de ejecución sintéticos permanecen en `pruebas/`, excluidos del seguimiento; las fuentes y evidencias del informe se conservan. La pestaña de navegador utilizada en la primera sesión ya no pertenecía a la sesión al reanudar la comprobación pendiente; se mantiene el límite de verificación visual indicado.
