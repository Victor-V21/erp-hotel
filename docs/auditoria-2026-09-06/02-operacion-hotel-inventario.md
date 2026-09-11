# Operación hotelera e inventario

### HOT-01 · P1 · Disponibilidad por fechas se mezcla con estado físico actual

**Ubicación:** [backend/src/hotel-erp.Api/Database/Repositories/RoomRepository.cs:30](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Repositories/RoomRepository.cs:30); [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:110](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:110)

**Evidencia y alcance:** T04: una habitación ocupada hoy se excluye de fechas futuras no solapadas. Crear una reserva futura cambia de inmediato el estado global a Reservada. La comprobación de solapamiento y el INSERT son separados.

**Impacto:** Se pierde capacidad de venta futura y dos solicitudes concurrentes pueden reservar el mismo intervalo. Un único servidor sigue atendiendo solicitudes concurrentes.

**Corrección propuesta:** Separar disponibilidad por intervalo y limpieza/mantenimiento físico; bloqueo por habitación o restricción PostgreSQL de exclusión para reservas activas, con límites de fecha [entrada, salida).

**Criterio de cierre:** Reservas adyacentes permitidas; solapadas concurrentes dejan exactamente una. La reserva del próximo mes no ocupa hoy el cuarto.

### HOT-02 · P1 · Transiciones de reserva permiten estados imposibles

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:146](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:146); [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:156](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:156); [backend/src/hotel-erp.Api/Dtos/common/HotelDtos.cs:87](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Dtos/common/HotelDtos.cs:87)

**Evidencia y alcance:** T05 cancela estadía en curso, T06 confirma la cancelada y T07 acepta editar solamente salida a una fecha anterior a entrada. T39 repetir check-in tras checkout acaba en 500 por folio único después de mutaciones.

**Impacto:** Habitación libre con huésped alojado, estadías negativas o reservaciones cerradas reabiertas sin reconciliación.

**Corrección propuesta:** Máquina de estados de servidor y validación del estado final tras aplicar PATCH; transiciones con versión/concurrencia y motivo. Repeticiones idempotentes.

**Criterio de cierre:** Matriz completa de transiciones válidas/invalidas; ninguna mutación ante una transición rechazada.

### HOT-03 · P1 · Check-in acepta sobrecapacidad y efectivo incoherente

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:176](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:176); [backend/src/hotel-erp.Api/Dtos/common/HotelDtos.cs:109](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Dtos/common/HotelDtos.cs:109)

**Evidencia y alcance:** T36: habitación de capacidad 2, adultos 10, recibido 1 y cambio 500 se registra con factura total 1190. No se liquida el anticipo 200. El endpoint no valida de forma completa disponibilidad, capacidad y coherencia del pago.

**Impacto:** Sobreocupación y documentos marcados pagados sin el cobro necesario. El anticipo queda como dato sin aplicación contable.

**Corrección propuesta:** Validar cupo y cuarto en la transacción; calcular cambio y saldo en servidor; registrar anticipo como pasivo/pago aplicado según política contable, nunca confiar en cambio del navegador.

**Criterio de cierre:** Rechazar 10 adultos en cupo 2; anticipo 200 sobre1190 deja saldo 990; recibido 1000 produce cambio 10 y asientos conciliados.

### HOT-04 · P1 · Check-out puede cerrar saldos pendientes o volver a facturar la estadía

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:340](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:340); [frontend/src/pages/reservations/CheckOutPage.tsx:128](/home/vm/Projects/erp-hotel/frontend/src/pages/reservations/CheckOutPage.tsx:128); [backend/src/hotel-erp.Api/Database/Entities/InvoiceEntities.cs:44](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Entities/InvoiceEntities.cs:44)

**Evidencia y alcance:** T38: folio 1305 se cierra con una sola factura 1190 del huésped, dejando consumo sin facturar. El frontend intenta facturar todas las líneas del folio pese a que check-in ya factura hospedaje; no existe vínculo de asignación factura↔línea de folio/reserva.

**Impacto:** Riesgo de pérdida de ingresos o doble facturación; consultar por huésped mezcla estadías distintas.

**Corrección propuesta:** Separar consumos, documentos y aplicaciones de pago; facturar solo líneas pendientes y cerrar cuando saldo/resolución autorizada sea cero. Enlazar factura y reserva/folio.

**Criterio de cierre:** Estadía 1190 facturada al ingreso +extra 115: salida emite solo115 y saldo 0; reintento no factura otra vez.

### HOT-05 · P1 · Descuentos y exoneraciones se aplican con reglas divergentes

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:23](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/ReservationsController.cs:23); [frontend/src/pages/reservations/CheckOutPage.tsx:47](/home/vm/Projects/erp-hotel/frontend/src/pages/reservations/CheckOutPage.tsx:47); [backend/src/hotel-erp.Api/Dtos/Discount/DiscountDtos.cs:28](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Dtos/Discount/DiscountDtos.cs:28)

**Evidencia y alcance:** Código: se suman valores de descuento sin interpretar consistentemente monto fijo/porcentaje, documentación, edad y alcance; se permiten valores de porcentaje superiores a 100 en el catálogo. La rama exonerada de check-in vuelve a aplicar descuento sobre una base ya descontada. Checkout usa tasa ||0.15, convirtiendo0 en 15%.

**Impacto:** Tarifas negativas o reducidas dos veces, impuestos distintos entre pantallas y servidor, descuentos sin evidencia.

**Corrección propuesta:** Una función de precio autoritativa de servidor, reglas de combinación, tipo, vigencia, elegibilidad y máximo; respuesta de cotización detallada usada por frontend.

**Criterio de cierre:** Probar descuento fijo50, porcentaje10, acumulación, huésped no elegible, base exonerada y tasa 0; misma cotización en reserva, factura y folio.

### HOT-06 · P1 · Borrado lógico oculta historia relacionada sin un criterio uniforme

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/SuppliersController.cs:73](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/SuppliersController.cs:73); [backend/src/hotel-erp.Api/Controllers/RoomsController.cs:76](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/RoomsController.cs:76); [backend/src/hotel-erp.Api/Database/ApplicationDbContext.cs:139](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/ApplicationDbContext.cs:139)

**Evidencia y alcance:** T21: borrar proveedor oculta compras aún contabilizadas. Relaciones en cascada más filtros lógicos afectan la visibilidad; Folio/FolioItem y Role no tienen los mismos filtros. Los endpoints de clientes y huéspedes sí tienen bloqueos específicos: no se les atribuye este defecto indiscriminadamente.

**Impacto:** Historial operativo/fiscal desaparece de consultas mientras sus saldos siguen vivos; habitaciones pueden eliminarse sin la misma protección que clientes.

**Corrección propuesta:** Desactivar maestros con historia; restringir borrado referencial y definir consultas históricas independientes de la vigencia del maestro. Revisar cada cascada y filtro con pruebas de regresión.

**Criterio de cierre:** Desactivar proveedor/cuarto conserva compras, reservas, folios y reportes; no quedan referencias invisibles o restauraciones parciales.

### INV-01 · P1 · Edición de stock evita el kardex y la concurrencia pierde salidas

**Ubicación:** [backend/src/hotel-erp.Api/Controllers/InventoryController.cs:81](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InventoryController.cs:81); [backend/src/hotel-erp.Api/Controllers/InventoryController.cs:167](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Controllers/InventoryController.cs:167)

**Evidencia y alcance:** T24: PUT de producto fija stock -10 sin movimiento. T40: dos salidas de 7 desde10 responden 200; saldo final 3 aunque el kardex registra14 unidades de salida.

**Impacto:** Existencias y movimientos no concilian. Inventario negativo o consumo no descontado aun en una sola máquina.

**Corrección propuesta:** Stock derivado de movimientos o actualizado atómicamente con condición stock suficiente y versión; ajustes con motivo/usuario, nunca PUT de saldo. Abrir existencias mediante movimiento inicial.

**Criterio de cierre:** Con 10 unidades, dos salidas simultáneas de 7 dejan una aceptada y otra conflicto; saldo 3, salida total 7. Edición directa de saldo prohibida.

### INV-02 · P2 · Inventario carece de integración de valoración y compras/consumos

**Ubicación:** [backend/src/hotel-erp.Api/Database/Entities/ProductEntities.cs:15](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Entities/ProductEntities.cs:15); [backend/src/hotel-erp.Api/Database/Entities/InvoiceEntities.cs:74](/home/vm/Projects/erp-hotel/backend/src/hotel-erp.Api/Database/Entities/InvoiceEntities.cs:74)

**Evidencia y alcance:** Modelo revisado: cantidades y precios de movimiento, pero sin vínculo sistemático compra→entrada→consumo→costo contable; líneas de factura no identifican producto y no hay unidad/conversión ni método de valoración.

**Impacto:** Sirve como registro aislado de cantidades; no permite confiar en costo de venta, inventario valorizado ni reposición conciliada con compras.

**Corrección propuesta:** Definir alcance requerido: si se gestionan artículos, integrar documentos, unidades, costo promedio/FIFO elegido y asientos. Si es solo control auxiliar, nombrarlo y limitar las promesas.

**Criterio de cierre:** Compra 10 a 5, compra 10 a 7 y salida5 producen existencias y costo según política, conciliados con mayor.

