# Índice de hallazgos

64 fichas agrupadas por causa, con criterios de aceptación en el capítulo enlazado. P0/P1/P2 son prioridades para este ERP local, no puntuaciones CVSS.

| ID | Prioridad | Hallazgo | Capítulo |
|---|---|---|---|
| FIN-01 | P0 | Las notas de crédito aumentan ingresos en lugar de revertirlos | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-02 | P0 | Las operaciones se guardan parcialmente antes de devolver un error | [Ver ficha](01-contabilidad-fiscal.md) |
| OPS-01 | P0 | El frontend actual no compila | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| OPS-02 | P0 | El Dockerfile de backend copia el código a una ruta distinta de la compilada | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| SEG-01 | P0 | Recepción puede convertirse en administrador modificando roles | [Ver ficha](03-seguridad.md) |
| SEG-02 | P0 | Credenciales iniciales y clave de firma están publicadas en el proyecto | [Ver ficha](03-seguridad.md) |
| DB-01 | P1 | Correlativo protegido por lock sigue usando el CAI obsoleto de EF | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| DB-02 | P1 | Inicio de rango y validación de autorización son inconsistentes | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| DB-03 | P1 | Mezcla de UTC, hora hondureña y límites de consulta | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| DB-04 | P1 | DTO devuelven líneas vacías y autorizaciones fallan al mapear fechas | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| FIN-03 | P1 | Las notas calculan impuestos desde LineTotal suministrado por el cliente | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-04 | P1 | Desglose fiscal de tasas y bases incorrecto | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-05 | P1 | Redondeo monetario inconsistente rompe la partida doble | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-06 | P1 | Notas fiscales editables dejan hash y asientos obsoletos | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-07 | P1 | Los libros suman documentos sin aplicar su naturaleza ni estado | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-08 | P1 | Pagar compras no registra la salida de dinero | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-09 | P1 | Compras duplicadas y borrado con saldos contradictorios | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-10 | P1 | Caja y arqueos no representan cobros reales | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-11 | P1 | Cuentas históricas y cierre contable carecen de protección efectiva | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-13 | P1 | La API permite facturas con líneas e importes negativos | [Ver ficha](01-contabilidad-fiscal.md) |
| FIN-14 | P1 | El borrador DMR-1 muestra un cálculo de ISV sin modelo de retenciones | [Ver ficha](01-contabilidad-fiscal.md) |
| HOT-01 | P1 | Disponibilidad por fechas se mezcla con estado físico actual | [Ver ficha](02-operacion-hotel-inventario.md) |
| HOT-02 | P1 | Transiciones de reserva permiten estados imposibles | [Ver ficha](02-operacion-hotel-inventario.md) |
| HOT-03 | P1 | Check-in acepta sobrecapacidad y efectivo incoherente | [Ver ficha](02-operacion-hotel-inventario.md) |
| HOT-04 | P1 | Check-out puede cerrar saldos pendientes o volver a facturar la estadía | [Ver ficha](02-operacion-hotel-inventario.md) |
| HOT-05 | P1 | Descuentos y exoneraciones se aplican con reglas divergentes | [Ver ficha](02-operacion-hotel-inventario.md) |
| HOT-06 | P1 | Borrado lógico oculta historia relacionada sin un criterio uniforme | [Ver ficha](02-operacion-hotel-inventario.md) |
| INV-01 | P1 | Edición de stock evita el kardex y la concurrencia pierde salidas | [Ver ficha](02-operacion-hotel-inventario.md) |
| OPS-03 | P1 | Nginx no entrega la aplicación y cambia las rutas de la API | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| OPS-04 | P1 | La impresión usa APIs de Windows dentro del destino Linux | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| OPS-05 | P1 | Recibo impreso no conserva fielmente el documento fiscal emitido | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| OPS-06 | P1 | Respaldo SQL no cubre todos los datos persistentes ni una recuperación del equipo | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| QA-01 | P1 | No hay una puerta automatizada que impida entregar código roto | [Ver ficha](06-calidad-codigo.md) |
| SEG-03 | P1 | Permisos declarados no se aplican a operaciones sensibles | [Ver ficha](03-seguridad.md) |
| SEG-04 | P1 | Recepción puede descargar un respaldo completo de PostgreSQL | [Ver ficha](03-seguridad.md) |
| SEG-05 | P1 | Desactivar un usuario no invalida su token de acceso | [Ver ficha](03-seguridad.md) |
| SEG-06 | P1 | Bitácora declara corrupta su propia cadena sin manipulación | [Ver ficha](03-seguridad.md) |
| SEG-07 | P1 | La auditoría no es completa ni resistente a modificaciones privilegiadas | [Ver ficha](03-seguridad.md) |
| SEG-08 | P1 | Puertos y privilegios exponen innecesariamente la base en la LAN | [Ver ficha](03-seguridad.md) |
| SEG-10 | P1 | Repositorios y lockfiles incluyen material sensible o vulnerable | [Ver ficha](03-seguridad.md) |
| UX-01 | P1 | Menú fijo impide trabajar con comodidad en pantallas estrechas | [Ver ficha](05-frontend-experiencia.md) |
| UX-02 | P1 | Contraste insuficiente en botones y estados de habitación | [Ver ficha](05-frontend-experiencia.md) |
| UX-03 | P1 | Etiquetas, iconos y selección contable no son accesibles por teclado | [Ver ficha](05-frontend-experiencia.md) |
| UX-04 | P1 | Modales carecen de un patrón consistente de foco y cierre | [Ver ficha](05-frontend-experiencia.md) |
| UX-05 | P1 | Botones de notas, cargos y autorizaciones no coinciden con el contrato API | [Ver ficha](05-frontend-experiencia.md) |
| UX-06 | P1 | Checkout calcula y envía datos fiscales incompatibles | [Ver ficha](05-frontend-experiencia.md) |
| UX-07 | P1 | La acción de ingresar una reserva no conserva la reserva seleccionada | [Ver ficha](05-frontend-experiencia.md) |
| UX-08 | P1 | Errores y cargas se presentan como cero, vacío o éxito | [Ver ficha](05-frontend-experiencia.md) |
| DB-05 | P2 | Listados completos y N+1 de reportes crecerán con el historial | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| DB-06 | P2 | Esquema requiere restricciones e índices de negocio | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| DB-07 | P2 | PostgreSQL ya está implementado, pero falta validar migración de datos históricos | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| FIN-12 | P2 | Catálogo y estados financieros necesitan reglas de consolidación explícitas | [Ver ficha](01-contabilidad-fiscal.md) |
| INV-02 | P2 | Inventario carece de integración de valoración y compras/consumos | [Ver ficha](02-operacion-hotel-inventario.md) |
| OPS-07 | P2 | Procesos de respaldo presentan carreras y fallos de disponibilidad | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| OPS-08 | P2 | Salud y arranque no reflejan preparación real | [Ver ficha](04-postgresql-docker-rendimiento.md) |
| QA-02 | P2 | Lógica de negocio duplicada en controladores y navegador | [Ver ficha](06-calidad-codigo.md) |
| QA-03 | P2 | Falta trazabilidad de entrega y manual de operación local | [Ver ficha](06-calidad-codigo.md) |
| SEG-09 | P2 | Endurecimiento de sesión, archivos y errores incompleto | [Ver ficha](03-seguridad.md) |
| UX-09 | P2 | Búsquedas y paginación generan carreras y trabajo innecesario | [Ver ficha](05-frontend-experiencia.md) |
| UX-10 | P2 | Navegación, permisos y atajos prometidos no están alineados | [Ver ficha](05-frontend-experiencia.md) |
| UX-11 | P2 | Acciones duplicables y validación inconsistente entre capas | [Ver ficha](05-frontend-experiencia.md) |
| UX-12 | P2 | La interfaz afirma funciones que no están implementadas o comprobadas | [Ver ficha](05-frontend-experiencia.md) |
| UX-13 | P2 | Previsualización de impresión ignora cambios aún no guardados | [Ver ficha](05-frontend-experiencia.md) |
| UX-14 | P2 | Tokens y movimiento necesitan una política común | [Ver ficha](05-frontend-experiencia.md) |
