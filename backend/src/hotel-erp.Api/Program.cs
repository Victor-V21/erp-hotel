using System.Text;
using System.Reflection;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Repositories;
using hotel_erp.Api.Services;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// DbContext - PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection("Backup"));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IGuestRepository, GuestRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IFolioRepository, FolioRepository>();
builder.Services.AddScoped<ICAIRepository, CAIRepository>();
builder.Services.AddScoped<IDocumentAuthorizationRepository, DocumentAuthorizationRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
builder.Services.AddScoped<ICashMovementRepository, CashMovementRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAccountingRepository, AccountingRepository>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<TaxService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IBusinessSettingsRepository, BusinessSettingsRepository>();
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
builder.Services.AddScoped<EscPosService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<IFiscalAuthorizationService, FiscalAuthorizationService>();
builder.Services.AddScoped<DatabaseBackupService>();
builder.Services.AddHostedService<BackupHostedService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });

    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:80")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("Frontend");
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

if (!app.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(app.Environment.WebRootPath) && Directory.Exists(app.Environment.WebRootPath))
{
    app.MapWhen(
        context => !context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/hubs"),
        spa =>
        {
            spa.UseDefaultFiles();
            spa.UseStaticFiles();
            spa.Run(async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
            });
        });
}

try
{
    Log.Information("Starting Hotel ERP API");

    // Ensure required directories exist
    var dataDir = Path.Combine(app.Environment.ContentRootPath, "Data");
    Directory.CreateDirectory(dataDir);
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Uploads", "authorizations"));

    // Apply pending migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Log.Information("Migrations applied successfully");

        // Seed Chart of Accounts
        if (!context.AccountingAccounts.Any())
        {
            var accounts = new List<AccountingAccount>
            {
                // Activo (1)
                new() { AccountNumber = "1", AccountName = "ACTIVO", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "11", AccountName = "Activo Circulante", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1101", AccountName = "Caja", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1102", AccountName = "Banco", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1103", AccountName = "Clientes por cobrar", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1104", AccountName = "Deudores varios", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "12", AccountName = "Activo Fijo", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1201", AccountName = "Mobiliario y equipo", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1202", AccountName = "Equipo de cómputo", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1203", AccountName = "Vehículos", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1204", AccountName = "Edificios", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1205", AccountName = "Terrenos", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "13", AccountName = "Otros Activos", AccountType = AccountType.Activo, IsActive = true },
                new() { AccountNumber = "1301", AccountName = "Inventario de suministros", AccountType = AccountType.Activo, IsActive = true },
                // Pasivo (2)
                new() { AccountNumber = "2", AccountName = "PASIVO", AccountType = AccountType.Pasivo, IsActive = true },
                new() { AccountNumber = "21", AccountName = "Pasivo Circulante", AccountType = AccountType.Pasivo, IsActive = true },
                new() { AccountNumber = "2101", AccountName = "ISV por pagar", AccountType = AccountType.Pasivo, IsActive = true },
                new() { AccountNumber = "2102", AccountName = "Tasa turística por pagar", AccountType = AccountType.Pasivo, IsActive = true },
                new() { AccountNumber = "2103", AccountName = "Proveedores", AccountType = AccountType.Pasivo, IsActive = true },
                new() { AccountNumber = "2104", AccountName = "Cuentas por pagar", AccountType = AccountType.Pasivo, IsActive = true },
                new() { AccountNumber = "22", AccountName = "Pasivo Largo Plazo", AccountType = AccountType.Pasivo, IsActive = true },
                new() { AccountNumber = "2201", AccountName = "Préstamos bancarios", AccountType = AccountType.Pasivo, IsActive = true },
                // Patrimonio (3)
                new() { AccountNumber = "3", AccountName = "PATRIMONIO", AccountType = AccountType.Patrimonio, IsActive = true },
                new() { AccountNumber = "3101", AccountName = "Capital", AccountType = AccountType.Patrimonio, IsActive = true },
                new() { AccountNumber = "3102", AccountName = "Utilidades retenidas", AccountType = AccountType.Patrimonio, IsActive = true },
                new() { AccountNumber = "3103", AccountName = "Resultado del ejercicio", AccountType = AccountType.Patrimonio, IsActive = true },
                // Ingreso (4)
                new() { AccountNumber = "4", AccountName = "INGRESO", AccountType = AccountType.Ingreso, IsActive = true },
                new() { AccountNumber = "4101", AccountName = "Ingresos por hospedaje", AccountType = AccountType.Ingreso, IsActive = true },
                new() { AccountNumber = "4102", AccountName = "Ingresos por servicios", AccountType = AccountType.Ingreso, IsActive = true },
                new() { AccountNumber = "4103", AccountName = "Ingresos por alimentos y bebidas", AccountType = AccountType.Ingreso, IsActive = true },
                new() { AccountNumber = "4104", AccountName = "Otros ingresos", AccountType = AccountType.Ingreso, IsActive = true },
                // Gasto (5)
                new() { AccountNumber = "5", AccountName = "GASTO", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5101", AccountName = "Sueldos y salarios", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5102", AccountName = "Servicios públicos", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5103", AccountName = "Alquileres", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5104", AccountName = "Mantenimiento y reparaciones", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5105", AccountName = "Publicidad y marketing", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5106", AccountName = "Impuestos y tasas", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5107", AccountName = "Depreciación", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5108", AccountName = "Gastos bancarios", AccountType = AccountType.Gasto, IsActive = true },
                new() { AccountNumber = "5109", AccountName = "Gastos varios", AccountType = AccountType.Gasto, IsActive = true },
            };
            context.AccountingAccounts.AddRange(accounts);
            context.SaveChanges();

            var allAccounts = context.AccountingAccounts.ToDictionary(a => a.AccountNumber, a => a);

            var parentMap = new Dictionary<string, string>
            {
                { "11", "1" },
                { "1101", "11" }, { "1102", "11" }, { "1103", "11" }, { "1104", "11" },
                { "12", "1" },
                { "1201", "12" }, { "1202", "12" }, { "1203", "12" }, { "1204", "12" }, { "1205", "12" },
                { "13", "1" },
                { "1301", "13" },
                { "21", "2" },
                { "2101", "21" }, { "2102", "21" }, { "2103", "21" }, { "2104", "21" },
                { "22", "2" },
                { "2201", "22" },
                { "3101", "3" }, { "3102", "3" }, { "3103", "3" },
                { "4101", "4" }, { "4102", "4" }, { "4103", "4" }, { "4104", "4" },
                { "5101", "5" }, { "5102", "5" }, { "5103", "5" }, { "5104", "5" },
                { "5105", "5" }, { "5106", "5" }, { "5107", "5" }, { "5108", "5" }, { "5109", "5" },
            };

            foreach (var (childNumber, parentNumber) in parentMap)
            {
                if (allAccounts.TryGetValue(childNumber, out var child) && allAccounts.TryGetValue(parentNumber, out var parent))
                {
                    child.ParentAccountId = parent.Id;
                }
            }

            context.SaveChanges();
            Log.Information("Chart of Accounts seeded: {Count} accounts", accounts.Count);
        }

        if (!context.Roles.Any())
        {
            var adminRole = new Role { Name = "Admin", Description = "Administrador del sistema" };
            var recepcionRole = new Role { Name = "Recepcion", Description = "Personal de recepción" };
            context.Roles.AddRange(adminRole, recepcionRole);
            context.SaveChanges();

            var permissions = new List<Permission>
            {
                new() { Name = "manage_users", Description = "Gestionar usuarios" },
                new() { Name = "manage_roles", Description = "Gestionar roles" },
                new() { Name = "manage_settings", Description = "Gestionar configuración" },
                new() { Name = "view_reports", Description = "Ver reportes" },
                new() { Name = "create_invoices", Description = "Crear facturas" },
                new() { Name = "manage_reservations", Description = "Gestionar reservaciones" }
            };
            context.Permissions.AddRange(permissions);
            context.SaveChanges();

            foreach (var permission in permissions)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
            }
            context.SaveChanges();

            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Email = "admin@hotelerp.com",
                FirstName = "Administrador",
                LastName = "Sistema",
                IsActive = true
            };
            context.Users.Add(adminUser);
            context.SaveChanges();

            context.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
            context.SaveChanges();

            Log.Information("Admin user, roles, and permissions seeded");
        }

        if (!context.BusinessSettings.Any())
        {
            context.BusinessSettings.Add(new BusinessSettings
            {
                BusinessName = "Hotel Maya Central",
                RTN = "08019012345678",
                Address = "Santa Rosa de Copán, Honduras",
                Phone = "9999-0000",
                Email = "info@hotelmayacentral.com",
                IsvRate = 0.15m,
                TouristTaxRate = 0.04m
            });
            context.SaveChanges();
            Log.Information("Business settings seeded");
        }

        if (!context.TaxConfigurations.Any())
        {
            context.TaxConfigurations.AddRange(
                new TaxConfiguration { Name = "ISV 15%", Rate = 0.15m, IsActive = true, ApplicableTo = "General" },
                new TaxConfiguration { Name = "Impuesto Turístico 4%", Rate = 0.04m, IsActive = true, ApplicableTo = "Hospedaje" }
            );
            context.SaveChanges();
            Log.Information("Tax configurations seeded");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
