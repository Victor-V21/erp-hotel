using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Auth & Security
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

        // Customers
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Guest> Guests => Set<Guest>();

        // Hotel
        public DbSet<RoomType> RoomTypes => Set<RoomType>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<Folio> Folios => Set<Folio>();
        public DbSet<FolioItem> FolioItems => Set<FolioItem>();

        // Billing SAR
        public DbSet<CAI> CAIs => Set<CAI>();
        public DbSet<DocumentAuthorization> DocumentAuthorizations => Set<DocumentAuthorization>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<CorrelativeLock> CorrelativeLocks => Set<CorrelativeLock>();
        public DbSet<TaxConfiguration> TaxConfigurations => Set<TaxConfiguration>();

        // Cash
        public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
        public DbSet<CashMovement> CashMovements => Set<CashMovement>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentApplication> PaymentApplications => Set<PaymentApplication>();
        public DbSet<Refund> Refunds => Set<Refund>();
        public DbSet<RefundApplication> RefundApplications => Set<RefundApplication>();
        public DbSet<CardSettlement> CardSettlements => Set<CardSettlement>();
        public DbSet<CardSettlementApplication> CardSettlementApplications => Set<CardSettlementApplication>();

        // Inventory
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

        // Accounting
        public DbSet<AccountingAccount> AccountingAccounts => Set<AccountingAccount>();
        public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
        public DbSet<EntryItem> EntryItems => Set<EntryItem>();

        // Business Settings
        public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();

        // Backups
        public DbSet<BackupLog> BackupLogs => Set<BackupLog>();

        // Discounts
        public DbSet<Discount> Discounts => Set<Discount>();

        // Purchases
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Auth composite keys
            modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // User - Role relationships
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Role - Permission relationships
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // RefreshToken
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            // AuditLog
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AuditLog>()
                .Property(al => al.Changes)
                .HasColumnType("TEXT");
            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Hash)
                .IsUnique();

            modelBuilder.Entity<IdempotencyRecord>().HasKey(record => record.Id);
            modelBuilder.Entity<IdempotencyRecord>().Property(record => record.Scope).HasMaxLength(64);
            modelBuilder.Entity<IdempotencyRecord>().Property(record => record.Key).HasMaxLength(64);
            modelBuilder.Entity<IdempotencyRecord>().Property(record => record.RequestHash).HasMaxLength(64);
            modelBuilder.Entity<IdempotencyRecord>()
                .HasIndex(record => new { record.UserId, record.Scope, record.Key })
                .IsUnique();

            // User unique indexes
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Role/Permission unique indexes
            modelBuilder.Entity<Role>().HasIndex(r => r.NormalizedName).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(r => r.SystemKey).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.Name).IsUnique();

            // Customer
            modelBuilder.Entity<Customer>().HasIndex(c => c.RTN).IsUnique();
            modelBuilder.Entity<Customer>()
                .Property(c => c.TaxpayerType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);

            // Guest
            modelBuilder.Entity<Guest>().HasIndex(g => g.DocumentNumber).IsUnique();
            modelBuilder.Entity<Guest>()
                .Property(g => g.TaxpayerType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<Guest>().HasQueryFilter(g => !g.IsDeleted);

            // Room
            modelBuilder.Entity<Room>().HasIndex(r => r.RoomNumber).IsUnique();
            modelBuilder.Entity<Room>()
                .Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId);
            modelBuilder.Entity<Room>().HasQueryFilter(r => !r.IsDeleted);

            // RoomType
            modelBuilder.Entity<RoomType>().HasIndex(rt => rt.Name).IsUnique();
            modelBuilder.Entity<RoomType>().HasQueryFilter(rt => !rt.IsDeleted);

            // Reservation
            modelBuilder.Entity<Reservation>()
                .Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
            modelBuilder.Entity<Reservation>()
                .Property(r => r.Version)
                .IsConcurrencyToken();
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Guest)
                .WithMany(g => g.Reservations)
                .HasForeignKey(r => r.GuestId);
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Room)
                .WithMany(r => r.Reservations)
                .HasForeignKey(r => r.RoomId);
            modelBuilder.Entity<Reservation>().HasQueryFilter(r => !r.IsDeleted);

            // Folio
            modelBuilder.Entity<Folio>()
                .HasIndex(f => f.ReservationId)
                .IsUnique();
            modelBuilder.Entity<Folio>()
                .Property(f => f.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
            modelBuilder.Entity<Folio>()
                .HasOne(f => f.Reservation)
                .WithOne(r => r.Folio)
                .HasForeignKey<Folio>(f => f.ReservationId);
            modelBuilder.Entity<Folio>()
                .HasOne(f => f.Guest)
                .WithMany(g => g.Folios)
                .HasForeignKey(f => f.GuestId);
            modelBuilder.Entity<Folio>()
                .HasOne(f => f.Room)
                .WithMany(r => r.Folios)
                .HasForeignKey(f => f.RoomId);
            modelBuilder.Entity<Folio>().HasQueryFilter(f => !f.IsDeleted);

            // FolioItem
            modelBuilder.Entity<FolioItem>()
                .HasOne(fi => fi.Folio)
                .WithMany(f => f.FolioItems)
                .HasForeignKey(fi => fi.FolioId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FolioItem>().HasQueryFilter(item => !item.IsDeleted);

            // CAI
            modelBuilder.Entity<CAI>().HasIndex(c => c.CAINumber).IsUnique();
            modelBuilder.Entity<CAI>()
                .Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<CAI>().HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<CorrelativeLock>().HasKey(l => l.LockName);
            modelBuilder.Entity<CorrelativeLock>()
                .Property(l => l.LockName)
                .HasMaxLength(100);

            // DocumentAuthorization
            modelBuilder.Entity<DocumentAuthorization>()
                .HasIndex(a => new { a.DocumentType, a.Status });
            modelBuilder.Entity<DocumentAuthorization>()
                .HasIndex(a => new { a.DocumentType, a.CAINumber })
                .IsUnique();
            modelBuilder.Entity<DocumentAuthorization>()
                .Property(a => a.DocumentType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<DocumentAuthorization>()
                .Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<DocumentAuthorization>().HasQueryFilter(a => !a.IsDeleted);

            // Invoice
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => new { i.CAIId, i.CorrelativeNumber })
                .IsUnique();
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => new { i.DocumentAuthorizationId, i.CorrelativeNumber })
                .IsUnique();
            modelBuilder.Entity<Invoice>()
                .Property(i => i.DocumentType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<Invoice>()
                .Property(i => i.TaxpayerType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.CAI)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CAIId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.DocumentAuthorization)
                .WithMany(a => a.Invoices)
                .HasForeignKey(i => i.DocumentAuthorizationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Guest)
                .WithMany(g => g.Invoices)
                .HasForeignKey(i => i.GuestId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Folio)
                .WithOne(f => f.SettlementInvoice)
                .HasForeignKey<Invoice>(i => i.FolioId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.FolioId)
                .IsUnique()
                .HasFilter("\"FolioId\" IS NOT NULL AND NOT \"IsDeleted\"");
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.AppliedDiscount)
                .WithMany()
                .HasForeignKey(i => i.AppliedDiscountId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.OriginalInvoice)
                .WithMany(i => i.CreditNotes)
                .HasForeignKey(i => i.OriginalInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Invoice>().HasQueryFilter(i => !i.IsDeleted);

            // InvoiceItem
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.InvoiceItems)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.OriginalInvoiceItem)
                .WithMany()
                .HasForeignKey(ii => ii.OriginalInvoiceItemId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InvoiceItem>()
                .HasIndex(ii => ii.OriginalInvoiceItemId);
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.FolioItem)
                .WithOne(fi => fi.InvoiceItem)
                .HasForeignKey<InvoiceItem>(ii => ii.FolioItemId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InvoiceItem>()
                .HasIndex(ii => ii.FolioItemId)
                .IsUnique()
                .HasFilter("\"FolioItemId\" IS NOT NULL AND NOT \"IsDeleted\"");
            modelBuilder.Entity<InvoiceItem>().HasQueryFilter(ii => !ii.IsDeleted);

            // CashMovement
            modelBuilder.Entity<CashMovement>()
                .Property(cm => cm.MovementType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<CashMovement>().Property(cm => cm.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<CashMovement>().Property(cm => cm.BalanceAfter).HasPrecision(18, 2);
            modelBuilder.Entity<CashMovement>().Property(cm => cm.ExpectedAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CashMovement>().Property(cm => cm.CountedAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CashMovement>().Property(cm => cm.Difference).HasPrecision(18, 2);
            modelBuilder.Entity<CashMovement>().Property(cm => cm.Notes).HasMaxLength(500);
            modelBuilder.Entity<CashMovement>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_CashMovements_Amount_NonNegative", "\"Amount\" >= 0");
                table.HasCheckConstraint("CK_CashMovements_Balance_NonNegative", "\"BalanceAfter\" >= 0");
            });
            modelBuilder.Entity<CashMovement>()
                .HasOne(cm => cm.CashRegister)
                .WithMany(cr => cr.CashMovements)
                .HasForeignKey(cm => cm.CashRegisterId);
            modelBuilder.Entity<CashMovement>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.CashMovements)
                .HasForeignKey(cm => cm.UserId);
            modelBuilder.Entity<CashMovement>()
                .HasIndex(cm => cm.ReferenceId)
                .IsUnique()
                .HasFilter("\"ReferenceId\" IS NOT NULL AND NOT \"IsDeleted\"");
            modelBuilder.Entity<CashMovement>()
                .HasIndex(cm => new { cm.CashRegisterId, cm.CreatedAt, cm.Id });
            modelBuilder.Entity<CashMovement>().HasQueryFilter(cm => !cm.IsDeleted);

            // CashRegister
            modelBuilder.Entity<CashRegister>().Property(cr => cr.Name).HasMaxLength(50);
            modelBuilder.Entity<CashRegister>().Property(cr => cr.Description).HasMaxLength(250);
            modelBuilder.Entity<CashRegister>()
                .HasIndex(cr => cr.Name)
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<CashRegister>().HasQueryFilter(cr => !cr.IsDeleted);

            // Payments and document applications
            modelBuilder.Entity<Payment>().Property(payment => payment.PaymentNumber).HasMaxLength(50);
            modelBuilder.Entity<Payment>().Property(payment => payment.Method).HasConversion<string>().HasMaxLength(20);
            modelBuilder.Entity<Payment>().Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30);
            modelBuilder.Entity<Payment>().Property(payment => payment.Currency).HasMaxLength(3);
            modelBuilder.Entity<Payment>().Property(payment => payment.ExternalReference).HasMaxLength(100);
            modelBuilder.Entity<Payment>().Property(payment => payment.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(payment => payment.CashReceived).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(payment => payment.CashChange).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>()
                .HasIndex(payment => payment.PaymentNumber)
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<Payment>().HasIndex(payment => payment.PaymentDate);
            modelBuilder.Entity<Payment>()
                .HasOne(payment => payment.RecordedByUser)
                .WithMany()
                .HasForeignKey(payment => payment.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(payment => payment.CashRegister)
                .WithMany()
                .HasForeignKey(payment => payment.CashRegisterId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(payment => payment.AccountingEntry)
                .WithOne()
                .HasForeignKey<Payment>(payment => payment.AccountingEntryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Payments_Amount_Positive", "\"Amount\" > 0");
                table.HasCheckConstraint("CK_Payments_Currency_HNL", "\"Currency\" = 'HNL'");
                table.HasCheckConstraint(
                    "CK_Payments_MethodFields",
                    "(\"Method\" = 'Efectivo' AND \"CashRegisterId\" IS NOT NULL AND \"CashReceived\" IS NOT NULL AND \"CashReceived\" >= \"Amount\" AND \"CashChange\" = \"CashReceived\" - \"Amount\" AND \"ExternalReference\" = '') OR " +
                    "(\"Method\" IN ('Tarjeta', 'Transferencia') AND \"CashRegisterId\" IS NULL AND \"CashReceived\" IS NULL AND \"CashChange\" IS NULL AND length(btrim(\"ExternalReference\")) >= 3)");
                table.HasCheckConstraint(
                    "CK_Payments_Status",
                    "\"Status\" IN ('Confirmado', 'Anulado', 'ParcialmenteReembolsado', 'Reembolsado')");
            });
            modelBuilder.Entity<Payment>().HasQueryFilter(payment => !payment.IsDeleted);

            modelBuilder.Entity<PaymentApplication>().Property(application => application.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<PaymentApplication>()
                .HasOne(application => application.Payment)
                .WithMany(payment => payment.Applications)
                .HasForeignKey(application => application.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentApplication>()
                .HasOne(application => application.Invoice)
                .WithMany(invoice => invoice.PaymentApplications)
                .HasForeignKey(application => application.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentApplication>()
                .HasIndex(application => new { application.PaymentId, application.InvoiceId })
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<PaymentApplication>().HasIndex(application => application.InvoiceId);
            modelBuilder.Entity<PaymentApplication>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_PaymentApplications_Amount_Positive", "\"Amount\" > 0");
            });
            modelBuilder.Entity<PaymentApplication>().HasQueryFilter(application => !application.IsDeleted);

            modelBuilder.Entity<Refund>().Property(refund => refund.RefundNumber).HasMaxLength(50);
            modelBuilder.Entity<Refund>().Property(refund => refund.Method).HasConversion<string>().HasMaxLength(20);
            modelBuilder.Entity<Refund>().Property(refund => refund.Status).HasConversion<string>().HasMaxLength(20);
            modelBuilder.Entity<Refund>().Property(refund => refund.Currency).HasMaxLength(3);
            modelBuilder.Entity<Refund>().Property(refund => refund.ExternalReference).HasMaxLength(100);
            modelBuilder.Entity<Refund>().Property(refund => refund.Reason).HasMaxLength(500);
            modelBuilder.Entity<Refund>().Property(refund => refund.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Refund>()
                .HasIndex(refund => refund.RefundNumber)
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<Refund>().HasIndex(refund => refund.RefundDate);
            modelBuilder.Entity<Refund>()
                .HasOne(refund => refund.Payment)
                .WithMany(payment => payment.Refunds)
                .HasForeignKey(refund => refund.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Refund>()
                .HasOne(refund => refund.RecordedByUser)
                .WithMany()
                .HasForeignKey(refund => refund.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Refund>()
                .HasOne(refund => refund.CashRegister)
                .WithMany()
                .HasForeignKey(refund => refund.CashRegisterId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Refund>()
                .HasOne(refund => refund.AccountingEntry)
                .WithOne()
                .HasForeignKey<Refund>(refund => refund.AccountingEntryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Refund>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Refunds_Amount_Positive", "\"Amount\" > 0");
                table.HasCheckConstraint("CK_Refunds_Currency_HNL", "\"Currency\" = 'HNL'");
                table.HasCheckConstraint(
                    "CK_Refunds_MethodFields",
                    "(\"Method\" = 'Efectivo' AND \"CashRegisterId\" IS NOT NULL AND \"ExternalReference\" = '') OR " +
                    "(\"Method\" IN ('Tarjeta', 'Transferencia') AND \"CashRegisterId\" IS NULL AND length(btrim(\"ExternalReference\")) >= 3)");
                table.HasCheckConstraint("CK_Refunds_Status", "\"Status\" IN ('Confirmado', 'Anulado')");
            });
            modelBuilder.Entity<Refund>().HasQueryFilter(refund => !refund.IsDeleted);

            modelBuilder.Entity<RefundApplication>().Property(application => application.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<RefundApplication>()
                .HasOne(application => application.Refund)
                .WithMany(refund => refund.Applications)
                .HasForeignKey(application => application.RefundId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RefundApplication>()
                .HasOne(application => application.CreditNote)
                .WithMany(note => note.RefundApplications)
                .HasForeignKey(application => application.CreditNoteId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RefundApplication>()
                .HasIndex(application => new { application.RefundId, application.CreditNoteId })
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<RefundApplication>().HasIndex(application => application.CreditNoteId);
            modelBuilder.Entity<RefundApplication>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_RefundApplications_Amount_Positive", "\"Amount\" > 0");
            });
            modelBuilder.Entity<RefundApplication>().HasQueryFilter(application => !application.IsDeleted);

            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.SettlementNumber).HasMaxLength(50);
            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.Currency).HasMaxLength(3);
            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.Status).HasConversion<string>().HasMaxLength(20);
            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.ExternalReference).HasMaxLength(100);
            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.GrossAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.BankDepositAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.CommissionAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CardSettlement>().Property(settlement => settlement.WithholdingAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CardSettlement>()
                .HasIndex(settlement => settlement.SettlementNumber)
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<CardSettlement>()
                .HasIndex(settlement => settlement.ExternalReference)
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<CardSettlement>().HasIndex(settlement => settlement.SettlementDate);
            modelBuilder.Entity<CardSettlement>()
                .HasOne(settlement => settlement.RecordedByUser)
                .WithMany()
                .HasForeignKey(settlement => settlement.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CardSettlement>()
                .HasOne(settlement => settlement.AccountingEntry)
                .WithOne()
                .HasForeignKey<CardSettlement>(settlement => settlement.AccountingEntryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CardSettlement>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_CardSettlements_GrossAmount_Positive", "\"GrossAmount\" > 0");
                table.HasCheckConstraint("CK_CardSettlements_Components_NonNegative", "\"BankDepositAmount\" >= 0 AND \"CommissionAmount\" >= 0 AND \"WithholdingAmount\" >= 0");
                table.HasCheckConstraint("CK_CardSettlements_Components_Total", "\"BankDepositAmount\" + \"CommissionAmount\" + \"WithholdingAmount\" = \"GrossAmount\"");
                table.HasCheckConstraint("CK_CardSettlements_Currency_HNL", "\"Currency\" = 'HNL'");
                table.HasCheckConstraint("CK_CardSettlements_Reference", "length(btrim(\"ExternalReference\")) >= 3");
                table.HasCheckConstraint("CK_CardSettlements_Status", "\"Status\" IN ('Confirmado', 'Anulado')");
            });
            modelBuilder.Entity<CardSettlement>().HasQueryFilter(settlement => !settlement.IsDeleted);

            modelBuilder.Entity<CardSettlementApplication>().Property(application => application.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<CardSettlementApplication>()
                .HasOne(application => application.CardSettlement)
                .WithMany(settlement => settlement.Applications)
                .HasForeignKey(application => application.CardSettlementId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CardSettlementApplication>()
                .HasOne(application => application.Payment)
                .WithMany(payment => payment.CardSettlementApplications)
                .HasForeignKey(application => application.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CardSettlementApplication>()
                .HasIndex(application => new { application.CardSettlementId, application.PaymentId })
                .IsUnique()
                .HasFilter("NOT \"IsDeleted\"");
            modelBuilder.Entity<CardSettlementApplication>().HasIndex(application => application.PaymentId);
            modelBuilder.Entity<CardSettlementApplication>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_CardSettlementApplications_Amount_Positive", "\"Amount\" > 0");
            });
            modelBuilder.Entity<CardSettlementApplication>().HasQueryFilter(application => !application.IsDeleted);

            // Product
            modelBuilder.Entity<Product>().HasIndex(p => p.SKU).IsUnique();
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);

            // Category
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);

            // InventoryMovement
            modelBuilder.Entity<InventoryMovement>()
                .Property(im => im.MovementType)
                .HasConversion<string>()
                .HasMaxLength(50);
            modelBuilder.Entity<InventoryMovement>()
                .HasOne(im => im.Product)
                .WithMany(p => p.InventoryMovements)
                .HasForeignKey(im => im.ProductId);
            modelBuilder.Entity<InventoryMovement>()
                .HasOne(im => im.User)
                .WithMany()
                .HasForeignKey(im => im.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<InventoryMovement>().HasQueryFilter(im => !im.IsDeleted);

            // AccountingAccount
            modelBuilder.Entity<AccountingAccount>().HasIndex(a => a.AccountNumber).IsUnique();
            modelBuilder.Entity<AccountingAccount>()
                .Property(a => a.AccountType)
                .HasConversion<string>()
                .HasMaxLength(50);
            modelBuilder.Entity<AccountingAccount>()
                .HasOne(a => a.ParentAccount)
                .WithMany(a => a.ChildAccounts)
                .HasForeignKey(a => a.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AccountingAccount>().HasQueryFilter(a => !a.IsDeleted);

            // AccountingEntry
            modelBuilder.Entity<AccountingEntry>()
                .Property(ae => ae.EntryType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<AccountingEntry>()
                .HasIndex(ae => ae.ReferenceId)
                .IsUnique()
                .HasFilter("\"ReferenceId\" IS NOT NULL AND NOT \"IsDeleted\"");
            modelBuilder.Entity<AccountingEntry>().HasQueryFilter(ae => !ae.IsDeleted);

            // EntryItem
            modelBuilder.Entity<EntryItem>()
                .HasOne(ei => ei.Entry)
                .WithMany(e => e.EntryItems)
                .HasForeignKey(ei => ei.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EntryItem>()
                .HasOne(ei => ei.Account)
                .WithMany(a => a.EntryItems)
                .HasForeignKey(ei => ei.AccountId);
            modelBuilder.Entity<EntryItem>().HasQueryFilter(ei => !ei.IsDeleted);

            // TaxConfiguration
            modelBuilder.Entity<TaxConfiguration>().HasIndex(tc => tc.Name).IsUnique();
            modelBuilder.Entity<TaxConfiguration>().HasQueryFilter(tc => !tc.IsDeleted);

            // Discount
            modelBuilder.Entity<Discount>()
                .Property(d => d.DiscountType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<Discount>().HasQueryFilter(d => !d.IsDeleted);

            // Supplier
            modelBuilder.Entity<Supplier>().HasIndex(s => s.RTN).IsUnique();
            modelBuilder.Entity<Supplier>().HasQueryFilter(s => !s.IsDeleted);

            // PurchaseInvoice
            modelBuilder.Entity<PurchaseInvoice>()
                .HasIndex(pi => pi.InvoiceNumber);
            modelBuilder.Entity<PurchaseInvoice>()
                .Property(pi => pi.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(pi => pi.Supplier)
                .WithMany(s => s.PurchaseInvoices)
                .HasForeignKey(pi => pi.SupplierId);
            modelBuilder.Entity<PurchaseInvoice>().HasQueryFilter(pi => !pi.IsDeleted);

            // PurchaseInvoiceItem
            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasOne(pii => pii.PurchaseInvoice)
                .WithMany(pi => pi.PurchaseInvoiceItems)
                .HasForeignKey(pii => pii.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PurchaseInvoiceItem>().HasQueryFilter(pii => !pii.IsDeleted);

            // Fiscal profile
            modelBuilder.Entity<BusinessSettings>()
                .Property(settings => settings.FiscalProfileStatus)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<BusinessSettings>()
                .HasIndex(settings => settings.FiscalProfileStatus);

            // BackupLog
            modelBuilder.Entity<BackupLog>()
                .Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Default values for timestamps (PostgreSQL compatible)
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.FindProperty("CreatedAt") != null)
                {
                    modelBuilder.Entity(entityType.Name).Property("CreatedAt").HasDefaultValueSql("NOW()");
                }
                if (entityType.FindProperty("UpdatedAt") != null)
                {
                    modelBuilder.Entity(entityType.Name).Property("UpdatedAt").HasDefaultValueSql("NOW()");
                }
            }

        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;

                if (entityEntry.State == EntityState.Added)
                {
                    ((BaseEntity)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
                }

                if (entityEntry.State == EntityState.Modified)
                {
                    var entity = (BaseEntity)entityEntry.Entity;
                    if (entity.IsDeleted && entity.DeletedAt == null)
                    {
                        entity.DeletedAt = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}
