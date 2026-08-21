# Plan de Correccion QA y Mejoras — Hotel ERP

## Contexto SAR Honduras

Normativa aplicable:
- **Acuerdo 481-2017** y reformas (609-2017, 725-2018, 817-2018): Reglamento del Regimen de Facturacion
- **Ley de ISV** (Decreto-Ley 24): Impuesto Sobre Ventas 15% general, 18% para servicios turisticos/hoteles
- **Tasa Turistica**: 4% sobre hospedaje (Ley de Equidad Tributaria)
- **DMC**: Declaracion Mensual de Compras (obligatorio presentar ante SAR)
- **Libro de Ventas y Libro de Compras**: Registros obligatorios para auditoria SAR

Documentos fiscales requeridos por SAR:
- Factura (con CAI o Autorizacion de Documento)
- Nota de Credito
- Nota de Debito
- Ticket (consumidor final)
- Libro de Ventas (reporte mensual)
- Libro de Compras (reporte mensual)
- Declaracion Jurada ISV mensual

---

## Bloque A — Correcciones Criticas (Backend)

### A1. BusinessSettingsRepository.UpdateAsync — perdida de 18 campos
**Archivo:** `Database/Repositories/SettingsRepository.cs`
**Problema:** Solo copia 7 de 25 campos al actualizar. Tasas de impuesto, config de impresion, toggles se revierten.
**Solucion:** Copiar TODOS los campos del entity en `UpdateAsync`.

### A2. RolesController — loop vacio en asignacion de permisos
**Archivo:** `Controllers/RolesController.cs:58-61`
**Problema:** El loop de permisos tiene cuerpo vacio. Los roles se crean sin permisos.
**Solucion:** Inyectar `ApplicationDbContext` o `RoleRepository` para persistir `RolePermission` records.

### A3. RegisterAsync no asigna rol default
**Archivo:** `Services/AuthService.cs:108`
**Problema:** Usuarios nuevos tienen 0 roles.
**Solucion:** Buscar rol "Recepcion" por defecto y asignarlo via `UserRole` record.

### A4. ForgotPassword / ResetPassword — NotImplementedException
**Archivo:** `Services/AuthService.cs:140-148`
**Problema:** Crash 500 en runtime.
**Solucion:** Implementar reset basico (generar token, permitir cambio de password). Sin email por ahora (single-machine), mostrar token en pantalla o log.

### A5. TaxConfigurationsController — 100% mock
**Archivo:** `Controllers/TaxConfigurationsController.cs`
**Problema:** Retorna datos hardcodeados con `Guid.NewGuid()`, sin DB.
**Solucion:** Crear `TaxConfigurationRepository` con CRUD real contra la tabla `TaxConfigurations`.

### A6. DeleteInvoiceItems no persiste
**Archivo:** `Database/Repositories/InvoiceRepository.cs:43-50`
**Problema:** Marca `IsDeleted=true` pero nunca llama `SaveChangesAsync()`.
**Solucion:** Agregar `await _context.SaveChangesAsync()` al final del metodo. Cambiar firma a `async Task`.

### A7. Registro abierto sin autorizacion
**Archivo:** `Controllers/AuthController.cs:35`
**Problema:** `POST /auth/register` accesible sin autenticacion.
**Solucion:** Agregar `[Authorize(Roles = "Admin")]` al endpoint de registro. Dejar login y refresh sin authorize.

### A8. Notas de credito/debito sin asiento contable
**Archivo:** `Controllers/InvoicesController.cs:255,354`
**Problema:** `CreateCreditNote` y `CreateDebitNote` no llaman `_accountingService.CreateInvoiceEntryAsync()`.
**Solucion:** Agregar llamada al servicio contable despues de crear la nota, igual que en `Create()`.

### A9. ISV en notas de credito/debito no aplica descuentos
**Archivo:** `Controllers/InvoicesController.cs:311,407`
**Problema:** `LineTotal` no descuenta `DiscountPercentage` antes de calcular ISV.
**Solucion:** Aplicar `lineTotal = quantity * unitPrice * (1 - discountPct/100)` antes del ISV.

### A10. TaxService con tasas hardcodeadas
**Archivo:** `Services/TaxService.cs:5-6`
**Problema:** ISV=0.15 y Turistica=0.04 hardcodeados, ignora `BusinessSettings`.
**Solucion:** Cargar tasas desde `BusinessSettings` en el constructor o via factory. Hacer `TaxService` scoped que lea de DB al inicio de cada request.

### A11. Lockout check en orden incorrecto
**Archivo:** `Services/AuthService.cs:37-68`
**Problema:** Verifica lockout DESPUES de incrementar intentos fallidos.
**Solucion:** Mover check de lockout al inicio, antes de validar password.

### A12. Seed data — Chart of Accounts sin jerarquia
**Archivo:** `Program.cs:191-246`
**Problema:** `ParentAccountId` nunca se setea. 41 cuentas sin relacion padre-hijo.
**Solucion:** Establecer jerarquia: "1"->"11"->"1101", "1"->"12"->"1201", etc.

### A13. Soft delete sin cascada
**Archivo:** `ApplicationDbContext.cs`
**Problema:** Borrar un Customer deja Invoices huerfanas visibles.
**Solucion:** Agregar interceptor en `SaveChanges` que propague `IsDeleted=true` y `DeletedAt` a hijos directos cuando un padre se soft-deleta.

### A14. PurchaseInvoicesController sin asiento contable
**Archivo:** `Controllers/PurchaseInvoicesController.cs`
**Problema:** Facturas de compra no generan entrada en libro mayor.
**Solucion:** Llamar `_accountingService` para crear asiento de compra (debito gastos/inventario, credito proveedores/caja).

### A15. ReservationsController.Update() no re-valida disponibilidad
**Archivo:** `Controllers/ReservationsController.cs:119`
**Problema:** Permite cambiar habitacion/fechas sin verificar conflictos.
**Solucion:** Agregar validacion de disponibilidad de habitacion para las nuevas fechas.

### A16. DataExportController ignora from/to en reservaciones
**Archivo:** `Controllers/DataExportController.cs:142`
**Problema:** Exporta todas las reservaciones sin filtrar por rango de fechas.
**Solucion:** Aplicar filtro `Where(r => r.CheckInDate >= from && r.CheckInDate <= to)`.

### A17. AccountingController N+1 queries
**Archivo:** `Controllers/AccountingController.cs:29-31`
**Problema:** Llama `GetAccountBalanceAsync` en loop.
**Solucion:** Crear metodo `GetAllAccountBalancesAsync()` en repository que retorne todos los balances en una sola query.

### A18. Seed data — agregar admin user y config basica
**Archivo:** `Program.cs`
**Problema:** Sin usuario admin, sin roles, sin permisos, sin tax config, sin business settings.
**Solucion:** Seed:
- Admin user (username: `admin`, password hasheada)
- Roles: Admin, Recepcion
- Permisos basicos
- BusinessSettings con valores por defecto reales
- TaxConfiguration con ISV 15% y Turistica 4%

### A19. Docker — arreglar para SQLite
**Archivo:** `docker/docker-compose.yml`
**Problema:** Define servicio PostgreSQL pero app usa SQLite.
**Solucion:** Eliminar servicio postgres, montar volumen para persistencia de SQLite (`./data:/app/Data`), ajustar connection string.

### A20. JWT Secret Key — mover a variable de entorno
**Archivos:** `appsettings.json`, `appsettings.Development.json`, `docker-compose.yml`
**Problema:** Secret hardcodeado en codigo fuente.
**Solucion:** Leer de `ASPNETCORE_JWT__SECRETKEY` env var. Documentar en docker-compose.

---

## Bloque B — Correcciones Frontend

### B1. Paleta de colores dorado + Montserrat
**Archivo:** `src/index.css`
**Solucion:**
- Importar Montserrat desde Google Fonts
- Cambiar variables CSS:
  - `--primary`: `#C69C4B` (dorado medio)
  - `--primary-foreground`: `#FFFFFF`
  - `--accent`: `#E8CD7B` (dorado claro)
  - `--foreground`: `#1A1A1A` (oscuro para texto)
  - `--muted`: `#F5E095` (dorado claro suave)
  - `--ring`: `#C69C4B`
  - `--border`: `#E8CD7B33` (dorado claro con alpha)
- Font-family: `'Montserrat', sans-serif`

### B2. Tasas de impuesto dinamicas (3 paginas)
**Archivos:** `RoomTypesPage.tsx`, `CheckInPage.tsx`, `InvoiceEditPage.tsx`
**Problema:** ISV=0.15 y Turistica=0.04 hardcodeados.
**Solucion:** Crear hook `useTaxRates()` que fetch `GET /settings/business` y retorne `{ isvRate, touristTaxRate }`. Usar en las 3 paginas.

### B3. Error handling global — try/catch en 18 paginas
**Problema:** 18 paginas con API calls sin try/catch.
**Solucion:**
- Crear hook `useApi()` wrapper con error handling automatico y toast notifications
- O agregar try/catch con toast a cada pagina (mas simple, mas repetitivo)
- Prioridad: RoomsPage, ReservationsPage, CheckInPage, InventoryPage, GuestsPage, InvoicesPage

### B4. Tipo Guest incompleto
**Archivo:** `types/index.ts:70-88`
**Problema:** Faltan `taxpayerType`, `exonerationOrderNumber`, `sefinExonerationCertificateNumber`, `sagRegistryNumber`.
**Solucion:** Agregar los 4 campos al tipo `Guest`. Eliminar casts `(g as any)` en `GuestsPage.tsx`.

### B5. Tipos centralizados
**Problema:** `Discount`, `Folio`, `FolioItem`, `BackupLog`, `GuestStats` definidos localmente en cada pagina.
**Solucion:** Mover todos a `types/index.ts`.

### B6. Eliminar archivos muertos
**Archivos:** `CAIPage.tsx`, `DocumentAuthorizationsPage.tsx`
**Solucion:** Eliminar ambos archivos (rutas ya redirigen a `/authorizations`).

### B7. Dashboard — agregar catch block
**Archivo:** `DashboardPage.tsx:23-31`
**Problema:** `try/finally` sin `catch`.
**Solucion:** Agregar catch con toast de error.

### B8. CashRegistersPage — agregar formulario de creacion
**Problema:** No hay UI para crear cajas registradoras.
**Solucion:** Agregar boton "Nueva Caja" con modal/form que llame `POST /cash-registers`.

### B9. ChartOfAccountsPage — editar/eliminar
**Problema:** Solo permite crear cuentas, no editar ni eliminar.
**Solucion:** Agregar botones de editar y desactivar (backend ya tiene PUT/DELETE).

### B10. Eliminar dependencias sin uso
- `recharts` — sin usar (quitar de package.json o implementar graficas en dashboard)
- Backend: `Hangfire`, `Hangfire.SQLite`, `FluentValidation.AspNetCore` — sin usar

### B11. Eliminar SignalR sin usar
**Archivo:** `Program.cs:134`
**Problema:** `AddSignalR()` configurado sin Hubs.
**Solucion:** Eliminar `AddSignalR()` y handler JWT para `/hubs`.

### B12. Habitaciones por piso — vista visual (NUEVO)
**Archivo:** Nuevo `FloorMapPage.tsx`
**Solucion:**
- Nueva pagina `/rooms/map` con vista de pisos
- Cada piso muestra habitaciones como cards/bloques
- Color por estado: verde=libre, rojo=ocupada, amarillo=reservada, gris=mantenimiento
- Icono por tipo: individual (1 cama), doble (2 camas), cuadruple (4 camas)
- Click en habitacion muestra detalle (huesped actual, check-in/out, precio)
- Agrupar por `Room.Floor` (necesita agregar campo `Floor` a Room entity si no existe)

---

## Bloque C — Reportes SAR y Contables

### C1. Libro de Ventas SAR (mejora del existente)
**Archivo:** `Controllers/ReportsController.cs`
**Estado actual:** Ya existe export XLSX basico.
**Mejoras:**
- Formato exacto del libro de ventas SAR (columnas: Fecha, Correlativo, CAI, RTN Cliente, Nombre, Gravado 15%, Gravado 18%, Exento, ISV 15%, ISV 18%, Total)
- Totales por columna
- Filtro por periodo fiscal (mes/ano)

### C2. Libro de Compras SAR (mejora del existente)
**Estado actual:** Ya existe export XLSX basico.
**Mejoras:**
- Formato DMC (Declaracion Mensual de Compras)
- Columnas: Fecha, No. Documento, RTN Proveedor, Nombre, Gravado, ISV, Total
- Separacion de compras con factura vs sin factura

### C3. Reporte de Impuestos a Pagar (NUEVO)
**Archivo:** Nuevo endpoint `GET /reports/tax-summary`
**Contenido:**
- Total ventas gravadas 15% del periodo
- Total ventas gravadas 18% del periodo (servicios turisticos)
- Total ventas exentas
- Total ISV 15% cobrado
- Total ISV 18% cobrado
- Total ISV en compras (credito fiscal)
- ISV neto a pagar (debito - credito)
- Tasa turistica recaudada
- Resumen mensual con comparacion periodo anterior

### C4. Reporte de Ocupacion (NUEVO)
**Endpoint:** `GET /reports/occupancy`
**Contenido:**
- Tasa de ocupacion por periodo (noches ocupadas / noches disponibles)
- Ingreso promedio por habitacion (ADR)
- Revenue por habitacion disponible (RevPAR)
- Desglose por tipo de habitacion

### C5. Reporte de Caja / Arqueo (NUEVO)
**Endpoint:** `GET /reports/cash-close`
**Contenido:**
- Apertura y cierre del dia
- Ingresos en efectivo
- Ingresos en tarjeta
- Gastos registrados
- Balance esperado vs real

### C6. Reporte de Auditoria (NUEVO)
**Endpoint:** `GET /reports/audit`
**Contenido:**
- Log de acciones por usuario y periodo
- Facturas emitidas/anuladas
- Cambios en configuracion
- Intentos de login fallidos

### C7. Vista previa vs impresion de factura (BUG)
**Problema:** Hay discrepancia entre la vista en pantalla y lo que se imprime.
**Solucion:** Unificar el formato de factura. La vista previa y la impresion deben usar el mismo componente/template. Verificar que `EscPosService` genere el mismo contenido que la vista.

---

## Bloque D — Configuracion y Limpieza

### D1. Eliminar archivo SQL desactualizado
**Archivo:** `database/migrations/001_initial_schema.sql`
**Solucion:** Eliminar o mover a `docs/archive/` con nota de deprecacion.

### D2. Eliminar archivo misterioso
**Archivo:** `hotel-erp/0`
**Solucion:** Eliminar.

### D3. Limpiar `using` duplicados
**Archivos:** ~15 archivos con `using` duplicados.
**Solucion:** Eliminar duplicados.

### D4. Limpiar `HasQueryFilter` duplicados
**Archivo:** `ApplicationDbContext.cs`
**Solucion:** Eliminar el bloque duplicado de lineas 408-423.

### D5. Unificar tipo DueDate (DateOnly vs DateTime)
**Archivos:** `InvoiceEntities.cs`
**Problema:** `CAI.DueDate` es `DateOnly`, `DocumentAuthorization.DueDate` es `DateTime`.
**Solucion:** Unificar ambos a `DateOnly` (las fechas de vencimiento SAR no necesitan hora). Requiere migracion.

### D6. Agregar campo Floor a Room entity
**Problema:** Para la vista de habitaciones por piso se necesita un campo `Floor`.
**Solucion:** Agregar `int Floor { get; set; }` a `Room`, crear migracion, agregar al frontend.

### D7. Eliminar dependencias NuGet sin uso
- `Hangfire` (v1.8.18)
- `Hangfire.SQLite` (v1.0.0)
- `FluentValidation.AspNetCore` (v11.3.0)

---

## Orden de Ejecucion Propuesto

| Prioridad | Bloque | Items | Estimado |
|-----------|--------|-------|----------|
| 1 | A (Critico backend) | A1, A2, A3, A5, A6, A7, A8, A9, A10 | Core fixes |
| 2 | B (Frontend base) | B1, B2, B4, B5, B6 | UI foundation |
| 3 | A (Backend restante) | A4, A11-A20 | Backend cleanup |
| 4 | B (Frontend restante) | B3, B7-B12 | UI features |
| 5 | C (Reportes SAR) | C1-C7 | Fiscal compliance |
| 6 | D (Limpieza) | D1-D7 | Code quality |

---

## Referencias Legales SAR

- Reglamento de Facturacion: Acuerdo 481-2017 + reformas (609-2017, 725-2018, 817-2018)
- Ley de ISV: Decreto-Ley 24 (consolidado hasta Decreto 59-2022)
- ISV General: 15%
- ISV Servicios Turisticos: 18% (hoteles)
- Tasa Turistica: 4% sobre hospedaje
- DMC: Declaracion Mensual de Compras (obligatoria)
- CAI: Control de Autorizacion de Impresion
- Validador SAR: https://oficinavirtual.sar.gob.hn/fac/validador-doc-fiscales/
