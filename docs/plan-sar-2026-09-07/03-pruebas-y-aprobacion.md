# Catálogo de pruebas y aprobación

**Estado inicial: todas las pruebas están pendientes.** Este es el contrato de aceptación de la futura implementación, no el resultado de una ejecución nueva. Las reproducciones de la auditoría original sirven como regresiones; algunas reproducían fallos y no deben contarse como aprobaciones.

[Plan principal](../auditoria-2026-09-06/08-plan-de-correccion.md) · [Modelo y ejemplos E01–E08](02-modelo-datos-y-transacciones.md) · [Perfil y fuentes](01-matriz-normativa-y-decisiones.md).

## Preparación y reglas de ejecución

1. Fijar SHA, esquema, versiones de dependencias/PostgreSQL, reglas fiscales y DEC aplicables. Preparar datos sintéticos deterministas, reloj controlado y usuarios de cada rol; separar totalmente del hotel real.
2. Unitarias: cálculo puro, asignación de descuentos/créditos, estados e invariantes. Los resultados esperados proceden de ejemplos independientes del contador, no del mismo método que se prueba.
3. Integración: API y PostgreSQL real de la versión principal acordada. No usar EF InMemory o SQLite para acreditar locks, precisión, restricciones o concurrencia de PostgreSQL. Se puede instalar PostgreSQL localmente; Docker no es requisito de esta etapa.
4. E2E: navegador sobre frontend compilado desde fuente y API de laboratorio; pruebas aisladas de componentes no sustituyen los flujos de cobro/emisión reales.
5. Windows 11: validar driver/impresora físicos con identidad de servicio, backup/restauración y tareas operativas. La auditoría en Linux y PostgreSQL18.6 no cubrió impresora ni PostgreSQL16.
6. Concurrencia: sincronizar inicio de solicitudes, usar claves iguales/distintas según caso y validar filas, importes y asientos al final; contar respuestas200 no prueba integridad.
7. Fallos: inyectar excepciones antes/después de escrituras/commit, pérdida de respuesta, disco lleno y caídas de servicios solo en laboratorio. Preservar logs seguros y SQL de verificación.
8. Los objetivos de velocidad/accesibilidad son criterios de ingeniería propuestos, no umbrales legales atribuidos al SAR. Acordar hardware y volumen en DEC-14; documentar p50/p95, caché, concurrencia y tamaño de datos.

Carpetas propuestas para la implementación: `backend/tests/hotel-erp.UnitTests`, `backend/tests/hotel-erp.IntegrationTests`, `frontend/tests` y `frontend/e2e`. Versionar el catálogo/fixtures sintéticos y guardar resultados por SHA en `docs/implementacion-sar/evidencia/<release>/`; evidencias sensibles fuera de Git con enlace protegido.

Para cada caso registrar: ID, regla/fixture, preparación, acción, esperado, resultado real, aprobado/fallido/bloqueado/no aplica, fecha, ejecutor, revisor y enlace al log/captura/SQL/PDF. «No aplica» requiere DEC firmada; no es un reemplazo de una prueba que falló. Si cambia el motor monetario, repetir MON/TX/NC/CTA y flujos afectados; ampliar regresión según impacto.

## Casos verificables

### Construcción y calidad

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| Q01 | Checkout identificado, dependencias fijadas | Instalar y compilar backend/frontend desde fuente | Cero errores; artefactos corresponden al SHA y no a bundles anteriores |
| Q02 | Frontend completo | Ejecutar tipos y lint | Cero errores; advertencias revisadas con disposición explícita |
| Q03 | Proyectos de prueba registrados | Ejecutar suite con un fallo deliberado de control en laboratorio y luego retirarlo | La puerta falla al detectar el defecto; no ignora tests ni devuelve éxito vacío |
| Q04 | Lockfiles y paquetes finales | Analizar dependencias y revisar advertencias backend | Sin riesgo crítico/alto aplicable pendiente; justificaciones de no aplicabilidad respaldadas y fechadas |
| Q05 | Casos de uso migrados | Revisar cálculo, controladores y repositorios | Un motor fiscal; ningún SaveChanges independiente rompe una operación compuesta |
| Q06 | Release listo | Reproducir instalación con manual y revisar archivos distribuidos | Versiones rastreables; sin SQLite/logs/credenciales reales ni código antiguo accidental |

### Perfil y obligaciones

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| N01 | Ficha del hotel | Contrastar RTN, registro, establecimiento y punto | Coinciden con evidencia vigente; modo real bloqueado si falta aprobación |
| N02 | Autorizaciones del hotel | Revisar modalidad, tipos, rangos y registro del sistema | Expediente completo o pendiente explícito; nunca aprobación SAR inferida |
| N03 | Listado de obligaciones activas | Mapear cada obligación a módulo o procedimiento externo | Todas tienen responsable, fuente, prueba y tratamiento; no aplica lleva sustento |
| N04 | Formulario y catálogo oficial vigentes | Comparar exportación con especificación y caso firmado | Campos, signos y totales coinciden; auxiliar no se anuncia como envío oficial |
| N05 | Impresora/papel elegidos | Revisar formato físico y expediente térmico si aplica | Requisitos y custodia comprobados; si falta trámite no se aprueba emisión por ese medio |
| N06 | Reglas y decisiones del contador | Revisar escenarios de tasas, descuentos, anticipos y moneda | DEC-01 a DEC-15 resueltas o no aplicables con evidencia; sin suposiciones silenciosas |

### Autorización y sesión

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| SEC01 | Usuario recepción y roles iniciales | Repetir renombrado de Admin y elevación T42 | 403; rol/permisos intactos; evento de intento registrado |
| SEC02 | Matriz de todos los endpoints | Invocar cada método sin autenticación | 401 salvo endpoints públicos expresamente justificados; sin efectos secundarios |
| SEC03 | Recepción/caja/contador/admin | Invocar métodos permitidos y prohibidos por rol | Permitidos funcionan; prohibidos 403; sin depender del menú |
| SEC04 | Permisos de administración | Intentar autoasignarse permisos, alterar rol protegido y reabrir sin autorización | Operaciones rechazadas y auditadas; no elevación por nombre editable |
| SEC05 | Dump y documentos privados existentes | Descargar como recepción, otro usuario y administrador autorizado | Sin acceso indebido; cada archivo corresponde al ID y permiso comprobados |
| SEC06 | Acceso y refresh vigentes | Desactivar usuario/cambiar contraseña y reutilizar ambos tokens | Acceso rechazado según política de revocación inmediata definida; no emisión posterior |
| SEC07 | Dos pestañas y sesión activa | Cerrar sesión y reutilizar refresh anterior | Logout servidor efectivo; cabecera/caché limpiadas y refresh revocado |
| SEC08 | Refresh rotado | Reutilizar token previo simultáneamente y enviar origen no permitido | Reutilización detectada; protección CSRF/CORS acorde con estrategia de sesión |
| SEC09 | Carga de archivo y ruta de descarga | Enviar sobredimensionado, contenido falso y ruta manipulada | Error controlado; sin escritura/lectura fuera del almacén autorizado |
| SEC10 | Login/errores de servicios | Forzar intentos repetidos, DB caída y excepción | Límite de intentos y ProblemDetails seguros; sin tokens, SQL sensible ni stacktrace |
| SEC11 | Credenciales de aplicación y migración | Intentar DDL, lectura privilegiada y modificación directa de bitácora con rol normal | Privilegios insuficientes; operaciones ordinarias necesarias siguen funcionando |

### Contratos

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| API01 | Factura, folio y compra con dos líneas | Consultar lista y detalle de cada uno | Detalle devuelve todas las líneas correctas; lista usa proyección documentada |
| API02 | Autorización con fechas civiles | Crear y leer autorización | No 500 posterior al insert; fechas YYYY-MM-DD conservadas |
| API03 | Enums inválidos y comprador empresarial válido | Enviar valores fuera de catálogo y opción UI válida | Inválidos rechazados antes de persistir; contrato UI/API compatible |
| API04 | PDF protegido y sesión válida | Descargar por mecanismo autenticado y luego sin sesión | Archivo correcto con sesión; sin acceso anónimo; URL temporal liberada |
| API05 | Contratos publicados | Comparar tipos/rutas/métodos con cliente | Tests detectan cambios incompatibles; ningún botón llama ruta 405 o cuerpo incompleto |

### Fechas y esquema

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| DAT01 | Instante UTC y fecha civil conocidos | Guardar/leer en Linux de laboratorio y Windows | Mismo instante y fecha fiscal hondureña; sin restar UTC dos veces |
| DAT02 | Movimientos 23:59:59 y 00:00 siguiente día/mes | Consultar días/meses contiguos | Cada movimiento aparece una sola vez en su período |
| DAT03 | Habitación y estadía con fechas sin hora | Serializar desde navegadores con zona distinta | Entrada/salida civiles no cambian por zona del cliente |
| DAT04 | Nueva base en versión PostgreSQL objetivo | Aplicar todas las migraciones | Esquema íntegro; precisión, índices y claves presentes |
| DAT05 | Datos inválidos de copia auditada | Ejecutar prevalidación y migración | Reporte de rechazos; sin eliminación/truncamiento silencioso |
| DAT06 | Proveedor/cuenta/habitación con historia | Desactivar y consultar documentos, mayor y auxiliares | No permite nuevo uso indebido; historia permanece visible y conciliada |
| DAT07 | Importes máximos y fraccionarios | Guardar cantidades/costos y operar restricciones | No desbordamientos ni truncamientos; errores controlados antes del commit |

### Documentos y autorizaciones

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| FIS01 | Autorización nueva con rango 100–102 | Emitir primera factura | Usa 100, no 101; LastIssued y documento coinciden |
| FIS02 | Mismo rango y tres facturas emitidas | Intentar cuarta emisión | Bloqueo por agotamiento sin operación parcial ni número fuera de rango |
| FIS03 | Autorización futura, vigente y vencida | Emitir en fechas de frontera con reloj controlado | Solo fecha permitida; regla inclusiva de límite aprobada respetada |
| FIS04 | Prefijos distintos y tipo incompatible | Crear rango/emisión que mezcla punto/tipo | Rechazo antes de persistir; no basta comparación lexicográfica |
| FIS05 | Perfiles/autorizaciones incompletos | Emitir con CAI ajeno, tipo de nota o perfil no aprobado | Operación rechazada; no asigna autorización de otro tipo |
| FIS06 | CAI próximo a agotarse/vencer | Consultar alertas y suspender autorización | Avisos correctos y emisión bloqueada al suspender; historia conservada |
| FIS07 | Rango con documentos no utilizados | Registrar motivo/trámite y revisar secuencia | Números identificados, evidencia preservada; no se reutilizan como si nunca existieron |
| FIS08 | Factura emitida | Intentar PUT/PATCH/DELETE monetario y cambio de líneas | Rechazo; snapshot, hash, asiento y número intactos |
| FIS09 | Nota emitida | Intentar editar total/motivo fiscal u origen | Rechazo; corrección solo mediante flujo permitido y trazable |
| FIS10 | Documento emitido con cliente/CAI actuales | Cambiar catálogos y volver a consultar/reimprimir | Mismos datos fiscales históricos y mismos totales |
| FIS11 | Proforma/recibo interno | Crear, imprimir y consultar libros | Identificación inequívoca; no consumo de rango de factura ni ingreso fiscal duplicado |
| FIS12 | Nota por endpoint genérico y datos originales incompletos | Intentar emisión | Rechazo y orientación al comando específico; sin nota huérfana |

### Concurrencia

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| CON01 | Autorización con capacidad para 50 ventas | Emitir 50 solicitudes simultáneas con claves distintas | 50 documentos únicos con asientos; sin contador EF obsoleto ni 500 evitable |
| CON02 | Queda un correlativo | Enviar dos emisiones simultáneas | Solo una confirma; otra informa agotamiento sin guardar datos parciales |
| CON03 | Mismo cuarto e intervalo | Confirmar dos reservas y luego probar cambio de habitación concurrente | Una ocupación válida; conflicto para la otra; no sobreventa |
| CON04 | Factura pendiente 115 y caja abierta | Aplicar simultáneamente dos pagos de 115 | Solo aplicación permitida; segunda no sobrecobra sin operación explícita de saldo a favor |
| CON05 | Stock 10 | Solicitar dos salidas simultáneas de 7 | Una salida 7; otra rechazada; stock 3 y kardex total salida 7 |

### Motor monetario

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| MON01 | E01 base 100 ISV 15 sin turismo | Calcular y emitir | Base 100, ISV al 15%: 15, ISV al 18%: 0, total 115 en todas las representaciones |
| MON02 | Base 100 ISV 18 sin turismo | Calcular y emitir | ISV al 18%: 18 y total 118; no coloca 18 en ISV 15 |
| MON03 | E05 cuatro líneas clasificadas | Emitir y consultar desglose | Base 15 100, base 18 100, exento 50, exonerado 50, ISV 33, total 333 |
| MON04 | Regla sintética turismo E05 y servicio no sujeto | Calcular cargos mixtos | Turismo solo del alojamiento; caso base 100 produce 4 bajo la regla de prueba |
| MON05 | Regla de prueba descuento 10% sobre base 100 ISV 15 | Calcular descuento autorizado | Descuento 10, base 90, ISV 13.50, total 103.50; descuento no se resta dos veces |
| MON06 | Beneficios con soporte válido, vencido y fuera de alcance | Cotizar/emitir casos por impuesto y línea | Solo aplica beneficio sustentado; exoneración ISV no elimina turismo automáticamente |
| MON07 | Precio inclusivo y precio neto equivalentes | Cotizar con política DEC-06 aprobada | Importes equivalentes según fixture firmado, sin residuos cargados arbitrariamente al ISV |
| MON08 | Base 0.03 y reglas E08 | Emitir múltiples líneas pequeñas | Encabezado, componentes y asiento concilian exactamente; nunca factura persistida sin asiento |
| MON09 | Cantidad/precio negativos y descuento mayor 100% | Intentar venta y editar catálogo | Rechazo antes del commit; no documento fiscal negativo |
| MON10 | Cliente envía LineTotal e impuesto manipulados | Emitir y crear nota con valores falsos | Servidor calcula desde datos autorizados o rechaza; no confía en totales del cliente |
| MON11 | Cambio de regla entre cotización y confirmación | Confirmar cotización antigua | Conflicto revisable; no cobro silencioso con monto diferente |
| MON12 | Tasa cero legítima y configuración ausente | Cotizar ambos escenarios | Cero se conserva; falta de configuración bloquea, no fallback 15% |

### Atomicidad e idempotencia

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| TX01 | Check-in necesita autorización y esta falta | Repetir T03 | Rechazo sin reserva ingresada, cuarto ocupado, folio ni pago residual |
| TX02 | Emisión con inyección de fallos | Fallar después de cada escritura antes de commit | Documento, número transaccional, pago, folio, asiento y evento se revierten juntos |
| TX03 | Solicitud confirmada pero respuesta perdida | Reenviar misma clave/cuerpo | Mismo ID/número/pago; un solo efecto financiero |
| TX04 | Una clave con dos cuerpos distintos | Enviar segunda solicitud | 409; conserva primera operación y no cambia su resultado |
| TX05 | Dos solicitudes simultáneas con misma clave | Confirmar operación concurrentemente | Una operación persistida y resultado recuperable; no duplicados |
| TX06 | Saldo acreditable único | Enviar dos notas que juntas lo exceden | Solo importe elegible confirmado; impuestos y saldo no negativos |
| TX07 | Impresora caída tras commit | Emitir y reintentar trabajo al recuperarla | Factura permanece emitida; mismo número en reimpresión; error separado del cobro |
| TX08 | Deadlock/transitorio simulado y clave de otro usuario | Reintentar y consultar resultado por usuario no autorizado | Retry acotado de toda transacción; sin doble efecto ni acceso cruzado |

### Notas y anulación

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| NC01 | E01 sin cobrar | Emitir crédito total | Ingreso/ISV netos cero; cliente cero; no crea caja ficticia |
| NC02 | E01 cobrada | Emitir crédito total sin devolver aún | Pasivo/saldo a favor 115 y caja 115; devolución pendiente explícita |
| NC03 | Crédito total de caso anterior | Reembolsar 115 y reintentar con misma clave | Caja neta cero y pasivo cero; un único reembolso |
| NC04 | E03 crédito 40+6 | Consultar original y saldo elegible | Original conservada; crédito 46 y saldo elegible 69 |
| NC05 | Saldo restante 69 | Emitir crédito 69 e intentar otro 0.01 | Segundo crédito válido; excedente rechazado; base/impuesto finales exactos |
| NC06 | Débito autorizado base 10 ISV 15 en fixture | Emitir nota de débito | Aumenta deuda 11.50 e impuesto 1.50; origen y motivo conservados |
| NC07 | Error de emisión y mes original cerrado | Ejecutar anulación/ajuste según casos firmados DEC-11 | Original/copia, número y motivo preservados; no modifica período cerrado por vía indirecta |

### Contabilidad

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| CTA01 | E01 venta y cobro | Consultar diario/mayor/cliente | Asientos 115 balanceados; ingreso 100, ISV 15, caja 115, cliente 0 |
| CTA02 | Asiento desbalanceado o cuenta no imputable | Intentar contabilizar directamente y desde venta | Rechazo completo; no asiento parcial ni venta que requiere asiento sin registrar |
| CTA03 | E04 anticipo condicionado | Registrar, aplicar y cobrar 65 | Caja 115, anticipo 0, cliente 0; ingreso e impuesto reconocidos una sola vez |
| CTA04 | E06 compra y dos pagos | Conciliar auxiliar y mayor | Proveedor 115→75→0; banco disminuye 115; inventario/impuesto correctamente separados |
| CTA05 | Asiento contabilizado | Intentar borrarlo y luego reversarlo autorizadamente | Borrado bloqueado; reverso enlazado y líneas sin huérfanos |
| CTA06 | Mismo evento/origen | Forzar doble contabilización por retry | Un asiento por evento; unicidad protegida en BD |
| CTA07 | Cuenta con historia desactivada | Generar mayor/balance del período previo | Movimientos y saldo siguen presentes; nuevas imputaciones prohibidas |
| CTA08 | Cuentas padre e hijos | Consolidar árbol de tres niveles | Padre suma descendientes una vez; balance general cuadra |
| CTA09 | Dos ejercicios con movimientos | Consultar año y cierre de resultado | Filtro anual real; saldos iniciales correctos y cuenta resultado según política aprobada |
| CTA10 | Período abierto y escritura concurrente | Cerrar mientras otra operación intenta contabilizar; luego reabrir sin permiso | Sin escritura no autorizada tras cierre; reapertura restringida y auditada |

### Operación hotelera

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| HOT01 | Cuarto libre hoy y reserva futura | Consultar disponibilidad hoy y en fechas reservadas | Disponible hoy; ocupado por reserva en su intervalo sin alterar estado físico actual |
| HOT02 | Reserva del 10 al 12 | Reservar del 12 al 14 y del 11 al 13 | Adyacente permitida; solapada rechazada |
| HOT03 | Reserva cancelada/salida | Intentar check-in, confirmación y fechas inválidas | Transiciones ilegales rechazadas; estado persistido coherente |
| HOT04 | Capacidad 2 y solicitud 10 adultos | Ingresar reserva | Validación de capacidad; no ocupación ni documentos parciales |
| HOT05 | Habitación mantenimiento y traslado | Reservar/ingresar/trasladar según política | Bloqueos y disponibilidad consistentes; traslado conserva estadía e historia |
| HOT06 | Reserva seleccionada existente | Abrir wizard e ingresar | Usa su ID; no crea segunda reserva; mantiene huésped correcto |
| HOT07 | Guardar huésped produce error | Intentar continuar wizard | No avanza como éxito; datos preservados y error visible |
| HOT08 | Folio 1305 y factura 1190 | Intentar salida sin tratar 115 pendientes | Bloqueo o tratamiento explícito autorizado; no saldo perdido |
| HOT09 | Estadía ya facturada al ingreso | Agregar consumo y salir | Factura solo cargo aún no asignado; no vuelve a cobrar estadía |
| HOT10 | Cliente empresarial a crédito y anticipo existente | Cerrar estancia con autorización y saldo documentado | Habitación liberada según política; deuda permanece en auxiliar, sin falso Pagada |

### Cobros y caja

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| PAG01 | Venta 115 y efectivo recibido 100 | Confirmar pago total y luego parcial explícito | Total insuficiente rechazado; parcial 100 deja saldo 15 si está permitido |
| PAG02 | Venta 115 efectivo 200 y cambio cliente 999 | Confirmar cobro | Cambio calculado 85; movimiento neto de caja 115 |
| PAG03 | Venta 115 con efectivo 50 y tarjeta 65 | Confirmar pago mixto | Aplicaciones 115; caja 50 y tarjeta 65; no efectivo 115 |
| PAG04 | Anticipo 50 y total 115 | Aplicar anticipo y liquidar | Saldo 65; no descuento fiscal adicional 50 |
| PAG05 | Caja apertura 1000 sin movimientos | Cerrar contado 1 enviando esperado 1 | Esperado servidor 1000 y diferencia−999; motivo/aprobación requeridos |
| PAG06 | Caja cerrada | Intentar cobro, egreso y reapertura sin permiso | Sin movimientos inválidos; error visible en frontend |
| PAG07 | E07 liquidación tarjeta 115 | Registrar banco 110, comisión 3 y retención 2 | Cuenta por liquidar 0; componentes 115; ingreso no reducido a110 |
| PAG08 | Arqueo con cobros, retiros y devoluciones | Cerrar/reimprimir/consultar caja | Esperado reconstruible desde movimientos; cierre inmutable y diferencias trazables |

### Compras

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| COM01 | Proveedor e invoice fiscal iguales | Crear dos compras secuenciales y simultáneas | Solo una compra por clave normalizada; sin doble deuda |
| COM02 | Compra con dos líneas y CAI/RTN requeridos | Guardar y consultar detalle | Líneas y campos completos; validación del comprobante documentada |
| COM03 | Compra mixta gasto/inventario/activo | Contabilizar según catálogo | Cuentas distintas correctas; no todo en gasto 5109 |
| COM04 | ISV acreditable/no acreditable/prorrata | Procesar fixtures del contador | Crédito y costo/gasto separados y conciliados con DMC/ISV |
| COM05 | Compra contabilizada | Intentar borrar y desactivar proveedor | No borrado; historia y saldo permanecen; corrección por reversión |
| COM06 | Compra 115 | Pagar 40 y75 desde UI | Dos movimientos bancarios/asientos; saldo 75 y luego 0 |
| COM07 | Devolución de compra inventariable | Registrar nota proveedor y devolver unidades | Deuda/impuestos/kardex/costo se ajustan una sola vez con referencias |

### Retenciones y obligaciones externas

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| RET01 | Proveedor sujeto y otro exceptuado con soporte | Calcular retención con regla vigente | Base/tasa/momento aprobados; excepción solo con sustento válido |
| RET02 | Deuda P y retención R | Pagar P−R y luego enterar R | Proveedor 0; pasivoR luego 0; banco disminuyeP entre ambos eventos |
| RET03 | Retención sufrida documentada | Registrar y conciliar con crédito tributario | Activo/auxiliar correcto; no confunde con retención practicada ni turismo |
| RET04 | Constancia/formulario obligatorio | Emitir y exportar con catálogos vigentes | Identidad/origen/importe/fecha correctos; autorización propia cuando corresponde |
| RET05 | Auxiliar externo nómina/ISR/activos | Importar lote válido, inválido y repetido | Contrato conciliado; rechazos explicados; sin duplicados ni obligación declarada cubierta sin procedimiento |

### Inventario y valoración

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| INV01 | Stock existente | Intentar editar Stock por PUT genérico | Rechazo; solo movimientos autorizados pueden alterarlo |
| INV02 | Compra recibida con producto/costo | Confirmar recepción y repetir operación | Una entrada kardex y valoración; no duplicación por retry |
| INV03 | Consumo cargado a folio | Facturarlo y cerrar estancia | Una salida de stock total; factura no repite el consumo |
| INV04 | Conteo físico y diferencia | Ajustar con/sin permiso y motivo | Solo ajuste autorizado con kardex y contrapartida contable |
| INV05 | Producto con costo histórico y precio cambiado | Consultar costo de venta y existencias | Valoración sigue método aprobado; precio comercial no reescribe costo histórico |
| INV06 | Devolución y reverso de movimiento | Procesar y conciliar mayor/kardex | Cantidad y valor reconstruibles; no borrado de movimientos originales |

### Libros y declaraciones

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| REP01 | Factura 115, crédito 46 y débito 11.50 | Generar libro por documento | Efecto neto 80.50; filas y tipos conservados; sin sumar crédito como venta positiva |
| REP02 | Original y anulación con reversión | Generar libros/diario | Una neutralización correcta, sin excluir original y restar dos veces |
| REP03 | Documento mixto E05 y compras deducibles | Generar auxiliar ISV por tasa/base | 15/18/exento/exonerado separados y crédito de compras sustentado |
| REP04 | Alojamiento y conceptos sin turismo | Generar auxiliar 259 | Base/importe solo aplicables; no mezcla con ISV o DMR |
| REP05 | Compras internas/importadas y notas aplicables | Generar DMC según contrato oficial aprobado | Campos, tipos, fechas, importes y totales de control correctos |
| REP06 | Retenciones practicadas y sufridas | Generar auxiliares y formularios del perfil | Cada obligación separada; no cálculo DMR a partir del ISV de ventas |
| REP07 | Declaración exportada sin presentar | Consultar estado, luego adjuntar acuse y pago | Preparada/revisada antes de evidencia; presentada/pagada solo con soporte; rectificación conserva historia |
| REP08 | Informe con filtros/fechas y exportación de texto | Comparar pantalla, archivo y mayor | Mismo alcance y total; formato versionado, codificación y delimitadores válidos |

### Bitácora

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| AUD01 | Evento/documento intacto | Guardar, reiniciar y verificar hash | Verifica sin falso positivo de precisión UTC/PostgreSQL |
| AUD02 | 50 mutaciones simultáneas | Verificar secuencia y cadena | Una cadena íntegra sin bifurcaciones; todos los eventos presentes |
| AUD03 | Cambio controlado de dato auditado en copia | Verificar integridad | Manipulación detectada con localización; no confundir con conversión de fecha |
| AUD04 | Permisos, pagos, notas, exportación y backup | Comparar operaciones con eventos | Actor, motivo, origen, correlación y resultados suficientes; sin secretos |
| AUD05 | Backup y ancla externa | Restaurar y verificar cadena/versiones | Cadena intacta válida; limitaciones privilegiadas documentadas, no promesa absoluta de inmutabilidad |

### Representación e impresión

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| IMP01 | Factura y notas emitidas | Renderizar PDF y salida térmica | Tipo, CAI/rango/vigencia, comprador, impuestos y referencias coinciden con snapshot |
| IMP02 | Ajustes de hotel que ocultan campos | Intentar suprimir bloques obligatorios | Configuración rechazada o bloque ignorado de forma explícita; documento fiscal completo |
| IMP03 | Nombres largos, ñ, acentos y varias líneas | Imprimir en dispositivo Windows real | Sin recortes ni caracteres incorrectos; totales legibles y copia fiel |
| IMP04 | Nueva configuración aún sin guardar | Generar vista previa | Todos los parámetros visibles aplicados; marca de muestra y sin nuevo número fiscal |
| IMP05 | Documento antiguo y cambio de CAI/tarifa/logo | Reimprimir | Información fiscal original preservada; reimpresión trazada y sin asiento nuevo |
| IMP06 | Impresora desconectada y spooler reiniciado | Emitir y recuperar trabajo | Error controlado; no revierte factura; reintento conserva ID/número |
| IMP07 | Sistema no Windows y cuenta de servicio Windows restringida | Enumerar/imprimir en ambos entornos | Error de capacidad controlado donde no soporte; permisos mínimos suficientes en Windows probado |

### Experiencia del usuario

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| UI01 | Factura válida con permiso de crédito | Crear nota desde botón real | Ruta/método/cuerpo válidos; muestra documento nuevo y saldo correcto |
| UI02 | Folio y autorización por fecha | Agregar cargo y crear autorización desde UI | FolioId y DateOnly correctos; sin 400/500 de contrato |
| UI03 | PDF con login | Abrir/descargar desde interfaz | Funciona autenticado y no expone URL insegura permanente |
| UI04 | Checkout con comprador empresarial y exoneración válida | Revisar y confirmar | Enum compatible, tasa cero preservada, datos del comprador correcto |
| UI05 | Confirmación monetaria y doble clic | Pulsar dos veces y reintentar tras timeout | Un resultado; indicador de envío y recuperación por clave |
| UI06 | Flujos compra/proveedor/retención aplicables | Completar desde menú con roles adecuados | Captura, validación, pago y consulta utilizables sin llamadas manuales API |
| UI07 | Red caída y formulario completo | Guardar/cargar datos | Error explícito; datos preservados; no cero/éxito falso |
| UI08 | Modal y selector contable | Completar tarea usando solo teclado | Foco visible, navegación, selección, cierre y restauración de foco correctos |
| UI09 | Inputs e iconos | Inspeccionar accesibilidad y lector de pantalla | Etiquetas asociadas, nombres accesibles y errores anunciados |
| UI10 | Viewport 390, escritorio y zoom 200% | Recorrer módulos principales | Sin desbordamiento global; acciones y cifras accesibles; tablas con scroll local cuando procede |
| UI11 | Temas y estados de habitaciones | Medir contraste y distinguir estados sin color | Objetivo 4.5:1 texto normal y3:1 grande; texto/icono además de color y foco visible |
| UI12 | Menú, breadcrumbs y paleta | Usar rutas/Enter con recepción y contador | Acciones implementadas, permisos alineados y destino correcto |
| UI13 | Sin internet y reducción de movimiento | Abrir aplicación y provocar error de render | Fuentes/recursos esenciales locales, movimiento reducido y recuperación mediante ErrorBoundary |
| UI14 | Búsqueda con respuestas fuera de orden | Escribir rápido varios términos | Última consulta determina resultados; solicitudes obsoletas canceladas/descartadas |
| UI15 | Más de 15 elementos | Cambiar página, tamaño y filtros | Paginación real de servidor; total y página coherentes |
| UI16 | Carga lenta y reintento | Consultar y enviar operaciones | Timeout visible; retry seguro; ninguna mutación duplicada por estrategia global |

### Volumen y velocidad

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| PERF01 | 100 mil documentos y10 usuarios simulados | Medir listados filtrados/paginados en equipo acordado | Objetivo inicial p95 ≤ 500 ms API sin impresión; página ≤ 200KB; registrar hardware y desviaciones |
| PERF02 | Consulta contable con muchas cuentas | Medir cantidad SQL y plan | Sin crecimiento N+1 por cuenta; agregación acotada y tiempo medido |
| PERF03 | Historial creciente y exportación | Comparar 10 mil/100 mil documentos | Memoria acotada y paginación efectiva; exportación no bloquea recepción |
| PERF04 | 50 escrituras concurrentes | Medir locks, deadlocks y latencia | Sin pérdida/duplicación; p95 emisión objetivo≤ 2 s excluyendo impresión; retries acotados |
| PERF05 | Servidor Windows con uso sostenido | Ejecutar jornada simulada y revisar recursos | Sin crecimiento continuo injustificado de memoria/conexiones; índices respaldados por medición |

### Continuidad local

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| OPS01 | Base caída o esquema incompatible | Consultar readiness e intentar emitir | No preparado y diagnóstico seguro; no health 200 engañoso ni emisión parcial |
| OPS02 | Arranque limpio y migración fallida | Iniciar servicio en ambos casos | Listo solo con precondiciones cumplidas; fallo no silenciado |
| OPS03 | DB, adjuntos y documentos conservados | Generar backup y comprobar manifest | Incluye componentes necesarios con hashes/versiones; sin credenciales expuestas |
| OPS04 | Dos backups en el mismo segundo | Solicitar trabajos y descargar cada ID | Archivos distintos y correctos; no descarga de último global |
| OPS05 | Ruta Windows con espacios y proceso colgado | Ejecutar backup/cancelación | Argumentos seguros, timeout y stderr capturado; no proceso huérfano ni shell injection |
| OPS06 | Disco lleno y destino externo desconectado | Ejecutar tarea programada | Fallo visible; no elimina último respaldo válido; recuperación/reintento controlados |
| OPS07 | Backup cifrado y equipo alterno | Restaurar con custodia de clave | Datos/archivos/historia verifican; RPO/RTO medidos contra acuerdo del hotel |
| OPS08 | Backup anterior a documentos ya entregados | Ensayar recuperación y reabrir emisión | Emisión bloqueada hasta reconciliar operaciones/números posteriores; cero reutilización fiscal |

### Migración

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| M01 | Copia identificada de origen real o sintético | Inventariar y restaurar antes de migrar | Procedencia y contenido conocidos; original preservado |
| M02 | Base auditada y migraciones nuevas | Actualizar esquema en copia | Instalación y actualización válidas en versión PostgreSQL objetivo |
| M03 | Datos históricos incompletos | Importar y repetir lote | Sin duplicados; rechazos/procedencia explícitos; ningún CAI o impuesto inventado |
| M04 | Corte ensayado y fallos | Restaurar conjunto DB/archivos/configuración | Recuperación completa cronometrada; hashes y saldo iguales al punto respaldado |
| M05 | Documentos y auxiliares migrados | Comparar conteos, sumas, Debe/Haber, stock y secuencia | Diferencias cero o ajuste individual documentado/aprobado; sin eliminación silenciosa |
| M06 | Rollback antes/después de nuevas emisiones | Ejecutar procedimientos separados | Antes recupera base coherente; después preserva/reconcilia todas las operaciones nuevas |

### Aceptación integral

| ID | Preparación / entrada | Acción | Resultado exigido |
|---|---|---|---|
| E2E01 | Reserva futura y anticipo | Completar reserva→ingreso→consumos→salida | Una estadía; impuestos según perfil; pagos/folio/asientos conciliados |
| E2E02 | Venta pagada y devolución parcial | Emitir crédito→reembolsar→reimprimir | Crédito y efectivo independientes y conciliados; original conservada |
| E2E03 | Folio con facturación parcial y crédito empresarial | Liquidar salida y consultar cartera | Sin cargos sin tratar ni doble factura; deuda autorizada visible |
| E2E04 | Compra inventariable y dos abonos | Recibir→consumir→pagar→conciliar | Proveedor, banco, kardex y mayor coinciden |
| E2E05 | Jornada con medios mixtos | Cerrar caja→libros→declaraciones→período | Totales coinciden al centavo y período cerrado impide cambios |
| E2E06 | Release candidata en Windows y copia migrada | Recepción opera, imprime, recupera y revisa primer cierre simulado | Actas técnica/contable/operativa con evidencia; Docker permanece pendiente |

## Puertas y responsables de aprobación

| Puerta | Evidencia mínima | Aprobadores | Motivo de rechazo |
|---|---|---|---|
| G0 | DEC completas, normas aplicables, perfil y ejemplos numéricos | Contador + administración | Perfil/tasa/momento/beneficio/obligación aplicable sin resolver |
| G1 | Build/lint, contratos, matriz de endpoints, secretos y revocación | Técnico revisor + QA | Código no compilable, elevación de privilegios o acceso indebido |
| G2 | MON, FIS, TX, NC, CTA básicos y migraciones de integridad | QA + contador + backend revisor | Diferencia de centavos, escritura parcial, duplicado, factura editable o secuencia incorrecta |
| G3 | HOT, PAG, COM, RET si aplica, INV y auxiliares conciliados | Recepción/caja/inventario + contador + QA | Saldo/stock perdido, cobro o factura duplicado, salida/pago sin tratamiento |
| G4 | REP, AUD, IMP y formatos comparados con fuentes vigentes | Contador + QA + operación Windows | Libro/representación incongruente, obligación sin soporte, historia no recuperable |
| G5 | UI, PERF, OPS y recuperación en equipo alterno | Usuarios del hotel + QA + operación | Tareas críticas inutilizables, secretos expuestos, restauración fallida o impresora no probada |
| G6 | Todas las puertas anteriores, M y E2E, actas, manuales y expediente | Administración + contador + responsable técnico | Cualquier P0/P1 funcional abierto, obligación aplicable sin atender o evidencia de aprobación ausente |
| GD, diferida | Instalación Docker final, LAN/proxy, persistencia, reinicios e impresión del conjunto | Operación + QA + administración | Se evalúa después; no marcar aprobada con pruebas nativas |

El rechazo de una puerta devuelve el trabajo al paso responsable; se corrige y repite la prueba fallida y regresión afectada. No es necesario repetir análisis masivos sin cambios, pero sí los controles de integridad tras cambios monetarios, de esquema o concurrencia.

## Acta reproducible de aprobación por release

Copiar y completar al implementar; **todos los campos están sin completar en este plan**.

| Campo | Valor a registrar |
|---|---|
| Release/SHA y fecha | Pendiente |
| Esquema, PostgreSQL, .NET, Node y sistema operativo | Pendiente |
| Perfil fiscal, modalidad, reglas y formularios | Pendiente |
| DEC aplicables aprobadas / no aplicabilidad sustentada | Pendiente |
| Pruebas previstas, ejecutadas, aprobadas, fallidas y bloqueadas | Pendiente; no usar el número de casos del catálogo como número de éxitos |
| Documentos/asientos/auxiliares revisados y diferencias | Pendiente |
| Impresora real, papel, original/copia y expediente | Pendiente |
| Migración y conciliación de saldos/correlativos | Pendiente |
| Respaldo restaurado, RPO/RTO acordados y medidos | Pendiente |
| Seguridad/dependencias y excepciones P2 con vencimiento | Pendiente |
| Firma técnica independiente y evidencia | Pendiente |
| Firma contable/fiscal interna y evidencia | Pendiente |
| Firma operativa del hotel/Windows y evidencia | Pendiente |
| Trámite/autorización SAR aplicable, número, fecha y resultado | Pendiente; aprobación interna no equivale a autorización externa |
| Alcance aprobado / limitaciones / Docker GD | Funcional local por comprobar; Docker diferido |

## Control del primer período real

Antes de primera emisión: verificar expediente, fecha del equipo, rango disponible, caja/usuarios, backup recuperable y release autorizada. Durante las primeras cinco jornadas: conciliar secuencia, documentos, cobros, caja y errores; investigar diferencias antes de cerrar. Al primer mes: reconstruir declaraciones desde auxiliares, revisar notas/anticipos/retenciones y archivar evidencia de presentación y pago. El responsable puede extender la observación si detecta fallos.

Si un incidente compromete integridad fiscal, detener la operación afectada, preservar evidencia y aplicar contingencia aprobada. No arreglar ventas reales editando filas ni reutilizando correlativos. Este documento define controles futuros; no activa tareas programadas, comunica al SAR ni ejecuta declaraciones.
