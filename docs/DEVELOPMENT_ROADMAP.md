# Roadmap de Desarrollo del Sistema ERP Hotelero y Contable

Este documento describe el plan de desarrollo paso a paso para el sistema ERP, siguiendo la arquitectura, stack tecnológico y prioridades definidas.

## Arquitectura General
*   **Estilo:** Clean Architecture, DDD (Domain Driven Design)
*   **Patrones:** Repository Pattern, Service Layer
*   **Evolución:** Modular Monolith inicialmente, preparado para microservicios futuros.

## Stack Tecnológico
*   **Backend:** C# SDK 8.0 (LTS), ASP.NET Core Web API, Entity Framework Core, PostgreSQL, JWT, SignalR, FluentValidation, AutoMapper, Serilog, Hangfire.
*   **Frontend:** React, TypeScript, Vite, React Router, React Query, Zustand, TailwindCSS, shadcn/ui, Axios, React Hook Form, Zod.
*   **Infraestructura:** Node.js, npm, Docker, Docker Compose, Nginx, Git.

## Fases de Desarrollo

### FASE 1: Core del Sistema (Módulos Esenciales)

**Objetivo:** Establecer la base del sistema con autenticación, gestión hotelera básica y facturación fiscal.

#### 1.1. Configuración de Proyectos y Entorno (Completado manualmente/previamente)
*   Creación de estructura de carpetas (backend, frontend, database, docker, docs, scripts).
*   Inicialización de proyectos .NET y React/Vite.
*   Configuración inicial de Docker y Docker Compose (PostgreSQL, API, Frontend, Nginx).
*   Esquema inicial de la base de datos.

#### 1.2. Módulo de Autenticación y Seguridad
*   **Backend:**
    *   Definición de entidades `User`, `Role`, `Permission` en `Domain`.
    *   Configuración de Entity Framework Core (contexto, migraciones).
    *   Implementación de `UserRepository` y `UserService`.
    *   Controladores de autenticación (`Login`, `Logout`, `Refresh Tokens`).
    *   Generación y validación de JWT.
    *   Hash de contraseñas.
    *   Implementación de roles y permisos granulares (basado en claims/policies).
    *   Bitácora de acciones y auditoría básica (`AuditLogs`).
    *   Manejo de errores y validaciones (FluentValidation).
*   **Frontend:**
    *   Configuración de `React Router` para rutas de autenticación.
    *   Página de `Login` (React Hook Form, Zod, Axios).
    *   Contexto/Store para manejo de estado de autenticación (Zustand).
    *   Protección de rutas.
    *   Componentes de UI con `shadcn/ui` y `TailwindCSS`.
*   **Pruebas:** Unit tests para servicios y controladores, Integration tests para el flujo de autenticación.

#### 1.3. Módulo Hotelero: Habitaciones
*   **Backend:**
    *   Definición de entidades `RoomType`, `Room` en `Domain`.
    *   `RoomTypeRepository`, `RoomRepository`, `RoomTypeService`, `RoomService`.
    *   CRUD para `RoomTypes` y `Rooms`.
    *   Endpoint para consultar estado de habitaciones.
*   **Frontend:**
    *   Páginas para `Gestión de Tipos de Habitación` y `Gestión de Habitaciones`.
    *   Formularios para CRUD, tablas avanzadas.
    *   Componentes para visualizar estados (colores).
*   **Pruebas:** Unit tests para la lógica de negocio, Integration tests para la persistencia.

#### 1.4. Módulo Hotelero: Reservaciones
*   **Backend:**
    *   Definición de entidad `Reservation` en `Domain`.
    *   `ReservationRepository`, `ReservationService`.
    *   Lógica para `Crear`, `Modificar`, `Cancelar`, `Confirmar` reserva.
    *   Implementación de `Check-in` (abrir folio, asignar habitación, consumo inicial).
    *   Implementación de `Check-out` (calcular noches, consumos, liberar habitación, cerrar folio).
    *   Validación de disponibilidad de habitaciones.
    *   Endpoints para `Reservations`, `Check-in`, `Check-out`.
*   **Frontend:**
    *   Página de `Reservaciones`.
    *   Formularios para CRUD de reservas.
    *   Páginas de `Check-in` y `Check-out` con flujos guiados.
    *   **Calendario Hotelero:** Vista visual (tipo PMS) con `drag & drop` (utilizando librerías de React para calendario).
*   **Pruebas:** Unit tests para lógica de reserva/check-in/check-out.

#### 1.5. Módulo de Facturación SAR Honduras
*   **Backend:**
    *   Definición de entidades `CAI`, `Invoice`, `InvoiceItem`, `TaxConfiguration` en `Domain`.
    *   Implementación de `CAIRepository`, `CAIService`.
    *   Lógica para `CAI` (validación de rango, vencimiento, correlativo).
    *   Generación automática de correlativo fiscal (transaccional, concurrencia segura).
    *   Servicio de `Facturación` con cálculo de ISV, Impuesto Turístico, descuentos (Tercera Edad).
    *   Endpoints para `Invoices`, `Notas de Crédito/Débito`, `Proformas`.
*   **Frontend:**
    *   Página de `Facturación`.
    *   Formulario para crear `Facturas`, `Notas` y `Proformas`.
    *   Integración con la lógica de `Check-out` para generar facturas automáticamente.
    *   Manejo de estados y alertas de `CAI`.
*   **Pruebas:** Unit tests para cálculos fiscales y lógica de correlativos, Integration tests para el flujo completo de facturación.

#### 1.6. Módulo de Caja y POS (Punto de Venta)
*   **Backend:**
    *   Definición de entidades `CashRegister`, `CashMovement` en `Domain`.
    *   `CashRegisterRepository`, `CashMovementRepository`, `CashRegisterService`, `CashMovementService`.
    *   Lógica para `Apertura`, `Cierre`, `Arqueo` de caja.
    *   Registro de `Ingresos` y `Egresos`.
    *   Integración con el módulo de `Facturación` para registrar pagos.
*   **Frontend:**
    *   Página `Punto de Venta (POS)` optimizada para velocidad (atajos de teclado, touch friendly).
    *   Página de `Gestión de Caja` (Apertura, Cierre, Arqueo, Movimientos).
    *   Reportes rápidos de caja.
*   **Pruebas:** Unit tests para lógica de caja y movimientos.


### FASE 2: Expansión de Funcionalidades (Módulos Adicionales)

**Objetivo:** Añadir inventario, reportes avanzados y contabilidad básica.

#### 2.1. Módulo de Clientes
*   **Backend:**
    *   Refinar entidades `Customer` (Fiscal) y `Guest` (Hotelero) en `Domain`.
    *   Unificar la gestión y permitir vinculación entre ambos tipos de perfiles.
    *   Lógica para `Historial de visitas`, `Preferencias`, `Clasificación VIP`, `Descuentos`.
    *   Endpoints para CRUD y consultas de clientes/huéspedes.
*   **Frontend:**
    *   Páginas `Gestión de Clientes` y `Gestión de Huéspedes`.
    *   Vistas de perfil detalladas.

#### 2.2. Módulo de Inventario
*   **Backend:**
    *   Definición de entidades `Product`, `Category`, `InventoryMovement` en `Domain`.
    *   `ProductRepository`, `InventoryMovementRepository`, `ProductService`, `InventoryService`.
    *   CRUD para `Productos` y `Categorías`.
    *   Gestión de `Stock` y `Kardex`.
    *   Lógica para `Compras` y `Ajustes` de inventario.
    *   Integración con `Punto de Venta`, `Restaurante` (futuro), `Minimarket`, `Room Service`.
*   **Frontend:**
    *   Páginas `Gestión de Productos`, `Gestión de Inventario`.
    *   Interfaz para realizar `Compras` y `Ajustes`.

#### 2.3. Módulo de Contabilidad
*   **Backend:**
    *   Definición de entidades `AccountingAccount`, `AccountingEntry`, `EntryItem` en `Domain`.
    *   `AccountingAccountRepository`, `AccountingEntryRepository`, `AccountingService`.
    *   Configuración y gestión de `Catálogo Contable`.
    *   Implementación de `Partidas Dobles`.
    *   Generación de `Diario General`, `Mayor`.
*   **Frontend:**
    *   Página de `Catálogo Contable`.
    *   Interfaz para `Registro de Partidas`.

#### 2.4. Módulo de Reportes
*   **Backend:**
    *   Servicio de `Reportes` que genere `PDF`, `Excel`.
    *   Implementación de reportes clave (Ventas diarias/mensuales, Ocupación, Impuestos, Clientes frecuentes, Cierre de caja, Auditoría, Habitaciones, Reservas, Historial fiscal).
*   **Frontend:**
    *   Página de `Reportes` con filtros y opciones de exportación.
    *   Visualización de reportes en interfaz.
    *   Manejo de `Impresión Térmica` para facturas.


### FASE 3: Optimizaciones y Escalabilidad

**Objetivo:** Mejorar rendimiento, añadir analíticas, preparación para la nube y soporte móvil.

#### 3.1. Dashboard Principal
*   **Backend:** Endpoints para métricas del dashboard.
*   **Frontend:** Implementación de `Dashboard` moderno (Habitaciones ocupadas/libres, Reservas del día, Ingresos diarios, Caja actual, Facturas emitidas, Check-ins/outs pendientes, Alertas SAR/CAI, Gráficas financieras).

#### 3.2. Analytics y Business Intelligence (Opcional)
*   Integración con herramientas de BI o desarrollo de funcionalidades de analíticas internas.

#### 3.3. Configuración para Cloud
*   Ajustes de infraestructura para despliegue en la nube (proveedor a definir).

#### 3.4. Aplicación Móvil (Conceptual/Futura)
*   Diseño y conceptualización de una posible aplicación móvil complementaria.


## Calidad del Código y Pruebas (Constante)
*   **Clean Code:** Todo el código debe ser limpio, escalable y modular.
*   **Documentación:** Comentarios, Swagger/OpenAPI para la API.
*   **Tipado:** Uso riguroso de TypeScript en frontend y tipos C# en backend.
*   **Pruebas:**
    *   Unit tests (Backend y Frontend).
    *   Integration tests (Backend).
    *   E2E tests (Cypress/Playwright - a definir).

## Entregables Obligatorios (Al finalizar cada fase principal)
*   Código fuente completo.
*   Scripts SQL (migraciones, seeders).
*   Docker Compose y Dockerfiles.
*   Manual de instalación.
*   Manual de usuario (básico por módulo).
*   Documentación de Swagger/OpenAPI.
*   Datos demo para pruebas.
*   Scripts de backup.
*   Scripts de producción.

## Próximos Pasos
La siguiente etapa será la implementación del **Módulo de Autenticación y Seguridad** para el backend, siguiendo el roadmap de la Fase 1.
