using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;
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
        public DbSet<TaxConfiguration> TaxConfigurations => Set<TaxConfiguration>();

        // Cash
        public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
        public DbSet<CashMovement> CashMovements => Set<CashMovement>();

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
                .HasColumnType("jsonb");
            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Hash)
                .IsUnique();

            // User unique indexes
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Role/Permission unique indexes
            modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
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

            // FolioItem
            modelBuilder.Entity<FolioItem>()
                .HasOne(fi => fi.Folio)
                .WithMany(f => f.FolioItems)
                .HasForeignKey(fi => fi.FolioId)
                .OnDelete(DeleteBehavior.Cascade);

            // CAI
            modelBuilder.Entity<CAI>().HasIndex(c => c.CAINumber).IsUnique();
            modelBuilder.Entity<CAI>()
                .Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

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
                .HasIndex(i => i.CorrelativeNumber)
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
                .HasOne(i => i.OriginalInvoice)
                .WithMany()
                .HasForeignKey(i => i.OriginalInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // InvoiceItem
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.InvoiceItems)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // CashMovement
            modelBuilder.Entity<CashMovement>()
                .Property(cm => cm.MovementType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<CashMovement>()
                .HasOne(cm => cm.CashRegister)
                .WithMany(cr => cr.CashMovements)
                .HasForeignKey(cm => cm.CashRegisterId);
            modelBuilder.Entity<CashMovement>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.CashMovements)
                .HasForeignKey(cm => cm.UserId);

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

            // AccountingEntry
            modelBuilder.Entity<AccountingEntry>()
                .Property(ae => ae.EntryType)
                .HasConversion<string>()
                .HasMaxLength(20);

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

            // TaxConfiguration
            modelBuilder.Entity<TaxConfiguration>().HasIndex(tc => tc.Name).IsUnique();

            // Discount
            modelBuilder.Entity<Discount>()
                .Property(d => d.DiscountType)
                .HasConversion<string>()
                .HasMaxLength(20);

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

            // BackupLog
            modelBuilder.Entity<BackupLog>()
                .Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Default values for timestamps
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.FindProperty("CreatedAt") != null)
                {
                    modelBuilder.Entity(entityType.Name).Property("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
                }
                if (entityType.FindProperty("UpdatedAt") != null)
                {
                    modelBuilder.Entity(entityType.Name).Property("UpdatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }

            // Soft delete filters for entities with IsDeleted
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.FindProperty("IsDeleted") != null
                    && entityType.ClrType.IsAssignableTo(typeof(BaseEntity)))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var filter = System.Linq.Expressions.Expression.Lambda(
                        System.Linq.Expressions.Expression.Equal(
                            System.Linq.Expressions.Expression.Property(parameter, "IsDeleted"),
                            System.Linq.Expressions.Expression.Constant(false)),
                        parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
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
            }
        }
    }
}

