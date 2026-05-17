-- Users Table (Módulo 1: Autenticación y Seguridad)
CREATE TABLE IF NOT EXISTS Users (
    Id UUID PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash TEXT NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    LastLogin TIMESTAMP,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Roles Table (Módulo 1: Autenticación y Seguridad)
CREATE TABLE IF NOT EXISTS Roles (
    Id UUID PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- UserRoles Table (Tabla intermedia para relación muchos a muchos entre Users y Roles)
CREATE TABLE IF NOT EXISTS UserRoles (
    UserId UUID NOT NULL,
    RoleId UUID NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

-- Permissions Table (Módulo 1: Autenticación y Seguridad)
CREATE TABLE IF NOT EXISTS Permissions (
    Id UUID PRIMARY KEY,
    Name VARCHAR(100) UNIQUE NOT NULL,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- RolePermissions Table (Tabla intermedia para relación muchos a muchos entre Roles y Permissions)
CREATE TABLE IF NOT EXISTS RolePermissions (
    RoleId UUID NOT NULL,
    PermissionId UUID NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);

-- AuditLogs Table (Módulo 1: Autenticación y Seguridad)
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id UUID PRIMARY KEY,
    UserId UUID,
    Action TEXT NOT NULL,
    EntityName VARCHAR(100),
    EntityId UUID,
    Changes JSONB,
    Timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

-- Customers Table (Módulo 4: Clientes - Cliente fiscal)
CREATE TABLE IF NOT EXISTS Customers (
    Id UUID PRIMARY KEY,
    RTN VARCHAR(14) UNIQUE,
    Name VARCHAR(100) NOT NULL,
    Address TEXT,
    Phone VARCHAR(20),
    Email VARCHAR(100),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Guests Table (Módulo 4: Clientes - Perfil hotelero)
CREATE TABLE IF NOT EXISTS Guests (
    Id UUID PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100),
    Phone VARCHAR(20),
    DateOfBirth DATE,
    Nationality VARCHAR(50),
    DocumentType VARCHAR(50),
    DocumentNumber VARCHAR(50) UNIQUE,
    Preferences TEXT,
    Classification VARCHAR(50), -- Ej: VIP, Frecuente
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- RoomTypes Table (Módulo 3: Hotelero - Habitaciones)
CREATE TABLE IF NOT EXISTS RoomTypes (
    Id UUID PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT,
    PricePerNight DECIMAL(10, 2) NOT NULL,
    Capacity INT NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Rooms Table (Módulo 3: Hotelero - Habitaciones)
CREATE TABLE IF NOT EXISTS Rooms (
    Id UUID PRIMARY KEY,
    RoomNumber VARCHAR(10) UNIQUE NOT NULL,
    Floor INT NOT NULL,
    RoomTypeId UUID NOT NULL,
    Status VARCHAR(50) NOT NULL, -- Libre, Ocupada, Limpieza, Mantenimiento, Reservada, Bloqueada
    Observations TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(Id)
);

-- Reservations Table (Módulo 3: Hotelero - Reservaciones)
CREATE TABLE IF NOT EXISTS Reservations (
    Id UUID PRIMARY KEY,
    GuestId UUID NOT NULL,
    RoomId UUID NOT NULL,
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    Adults INT NOT NULL DEFAULT 1,
    Children INT NOT NULL DEFAULT 0,
    PaymentMethod VARCHAR(50),
    AdvancePayment DECIMAL(10, 2) DEFAULT 0.00,
    Status VARCHAR(50) NOT NULL, -- Pendiente, Confirmada, CheckIn, CheckOut, Cancelada
    Notes TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (GuestId) REFERENCES Guests(Id),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);

-- Folios Table (Módulo 3: Hotelero - Cuentas de hotel)
CREATE TABLE IF NOT EXISTS Folios (
    Id UUID PRIMARY KEY,
    ReservationId UUID UNIQUE NOT NULL,
    GuestId UUID NOT NULL,
    RoomId UUID NOT NULL,
    OpeningDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ClosingDate TIMESTAMP,
    TotalAmount DECIMAL(10, 2) DEFAULT 0.00,
    Status VARCHAR(50) NOT NULL, -- Abierto, Cerrado, PendientePago
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ReservationId) REFERENCES Reservations(Id),
    FOREIGN KEY (GuestId) REFERENCES Guests(Id),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);

-- CAI Table (Módulo 5: Facturación SAR Honduras)
CREATE TABLE IF NOT EXISTS CAI (
    Id UUID PRIMARY KEY,
    CAINumber VARCHAR(20) UNIQUE NOT NULL,
    IssueDate DATE NOT NULL,
    DueDate DATE NOT NULL,
    InitialRange VARCHAR(20) NOT NULL,
    FinalRange VARCHAR(20) NOT NULL,
    CurrentCorrelative VARCHAR(20) NOT NULL,
    Status VARCHAR(20) NOT NULL, -- Activo, Vencido, Agotado
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Invoices Table (Módulo 5: Facturación SAR Honduras)
CREATE TABLE IF NOT EXISTS Invoices (
    Id UUID PRIMARY KEY,
    CAIId UUID NOT NULL,
    CorrelativeNumber VARCHAR(20) UNIQUE NOT NULL,
    InvoiceDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CustomerId UUID NOT NULL,
    GuestId UUID, -- Para facturas generadas desde un folio hotelero
    RTNCliente VARCHAR(14),
    CustomerName VARCHAR(100) NOT NULL,
    CustomerAddress TEXT,
    SubTotal DECIMAL(10, 2) NOT NULL,
    ISVAmount DECIMAL(10, 2) NOT NULL,
    TouristTaxAmount DECIMAL(10, 2) DEFAULT 0.00,
    DiscountsAmount DECIMAL(10, 2) DEFAULT 0.00,
    TotalAmount DECIMAL(10, 2) NOT NULL,
    DocumentType VARCHAR(20) NOT NULL, -- Factura, Recibo, NotaCredito, NotaDebito, Proforma
    Status VARCHAR(20) NOT NULL, -- Emitida, Pagada, Anulada
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CAIId) REFERENCES CAI(Id),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (GuestId) REFERENCES Guests(Id)
);

-- InvoiceItems Table (Detalle de cada factura)
CREATE TABLE IF NOT EXISTS InvoiceItems (
    Id UUID PRIMARY KEY,
    InvoiceId UUID NOT NULL,
    Description TEXT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10, 2) NOT NULL,
    LineTotal DECIMAL(10, 2) NOT NULL,
    IsExempt BOOLEAN NOT NULL DEFAULT FALSE,
    ISVRate DECIMAL(5, 2) NOT NULL DEFAULT 0.15, -- Porcentaje de ISV aplicado
    IsTouristTaxable BOOLEAN NOT NULL DEFAULT FALSE,
    DiscountPercentage DECIMAL(5, 2) DEFAULT 0.00, -- Descuento por línea (ej: tercera edad)
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE
);

-- TaxConfigurations Table (Módulo 6: Impuestos Honduras)
CREATE TABLE IF NOT EXISTS TaxConfigurations (
    Id UUID PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Rate DECIMAL(5, 2) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    ApplicableTo VARCHAR(100), -- Ej: Hospedaje, Restaurante
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- CashRegisters Table (Módulo 7: Caja y POS)
CREATE TABLE IF NOT EXISTS CashRegisters (
    Id UUID PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- CashMovements Table (Módulo 7: Caja y POS)
CREATE TABLE IF NOT EXISTS CashMovements (
    Id UUID PRIMARY KEY,
    CashRegisterId UUID NOT NULL,
    UserId UUID NOT NULL,
    MovementType VARCHAR(20) NOT NULL, -- Apertura, Cierre, Ingreso, Egreso, Arqueo
    Amount DECIMAL(10, 2) NOT NULL,
    Description TEXT,
    MovementDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    BalanceAfter DECIMAL(10, 2) NOT NULL,
    ReferenceId UUID, -- Para vincular con facturas, etc.
    FOREIGN KEY (CashRegisterId) REFERENCES CashRegisters(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Products Table (Módulo 8: Inventario)
CREATE TABLE IF NOT EXISTS Products (
    Id UUID PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    SKU VARCHAR(50) UNIQUE,
    CategoryId UUID,
    UnitPrice DECIMAL(10, 2) NOT NULL,
    CurrentStock INT NOT NULL DEFAULT 0,
    MinStockLevel INT DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Categories Table (Para Productos)
CREATE TABLE IF NOT EXISTS Categories (
    Id UUID PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE Products ADD CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id);

-- InventoryMovements Table (Módulo 8: Inventario - Kardex, Compras, Ajustes)
CREATE TABLE IF NOT EXISTS InventoryMovements (
    Id UUID PRIMARY KEY,
    ProductId UUID NOT NULL,
    MovementType VARCHAR(50) NOT NULL, -- Entrada, Salida, Ajuste, Compra, Venta
    Quantity INT NOT NULL,
    MovementDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UnitPrice DECIMAL(10, 2),
    TotalValue DECIMAL(10, 2),
    NewStock INT NOT NULL,
    ReferenceId UUID, -- Para vincular con facturas, compras, etc.
    UserId UUID, -- Quien realizó el movimiento
    FOREIGN KEY (ProductId) REFERENCES Products(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

-- AccountingEntries Table (Módulo 9: Contabilidad)
CREATE TABLE IF NOT EXISTS AccountingEntries (
    Id UUID PRIMARY KEY,
    TransactionDate DATE NOT NULL,
    Description TEXT NOT NULL,
    EntryType VARCHAR(20) NOT NULL, -- Diario, Ajuste, Cierre
    ReferenceId UUID, -- Para vincular con facturas, movimientos de caja, etc.
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- AccountingAccounts Table (Catálogo contable)
CREATE TABLE IF NOT EXISTS AccountingAccounts (
    Id UUID PRIMARY KEY,
    AccountNumber VARCHAR(20) UNIQUE NOT NULL,
    AccountName VARCHAR(100) NOT NULL,
    AccountType VARCHAR(50) NOT NULL, -- Activo, Pasivo, Patrimonio, Ingreso, Gasto
    ParentAccountId UUID,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ParentAccountId) REFERENCES AccountingAccounts(Id)
);

-- EntryItems Table (Detalle de cada partida contable - Partidas dobles)
CREATE TABLE IF NOT EXISTS EntryItems (
    Id UUID PRIMARY KEY,
    EntryId UUID NOT NULL,
    AccountId UUID NOT NULL,
    Debit DECIMAL(10, 2) DEFAULT 0.00,
    Credit DECIMAL(10, 2) DEFAULT 0.00,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (EntryId) REFERENCES AccountingEntries(Id) ON DELETE CASCADE,
    FOREIGN KEY (AccountId) REFERENCES AccountingAccounts(Id)
);

-- Índices para mejorar el rendimiento
CREATE INDEX idx_users_username ON Users (Username);
CREATE INDEX idx_users_email ON Users (Email);
CREATE INDEX idx_auditlogs_userid ON AuditLogs (UserId);
CREATE INDEX idx_auditlogs_timestamp ON AuditLogs (Timestamp);
CREATE INDEX idx_customers_rtn ON Customers (RTN);
CREATE INDEX idx_guests_documentnumber ON Guests (DocumentNumber);
CREATE INDEX idx_rooms_roomnumber ON Rooms (RoomNumber);
CREATE INDEX idx_reservations_checkindate ON Reservations (CheckInDate);
CREATE INDEX idx_reservations_checkoutdate ON Reservations (CheckOutDate);
CREATE INDEX idx_folios_reservationid ON Folios (ReservationId);
CREATE INDEX idx_cai_cainumber ON CAI (CAINumber);
CREATE INDEX idx_invoices_correlativenumber ON Invoices (CorrelativeNumber);
CREATE INDEX idx_invoices_invoicedate ON Invoices (InvoiceDate);
CREATE INDEX idx_products_sku ON Products (SKU);
CREATE INDEX idx_inventorymovements_productid ON InventoryMovements (ProductId);
CREATE INDEX idx_accountingentries_transactiondate ON AccountingEntries (TransactionDate);
CREATE INDEX idx_accountingaccounts_accountnumber ON AccountingAccounts (AccountNumber);
