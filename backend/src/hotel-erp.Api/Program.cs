using System.Text;
using System.Reflection;
using System.Security.Claims;
using System.Globalization;
using System.Threading.RateLimiting;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Repositories;
using hotel_erp.Api.Services;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings__DefaultConnection fuera del repositorio antes de iniciar la API.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
var databaseStartupOptions = builder.Configuration
    .GetSection(DatabaseStartupOptions.SectionName)
    .Get<DatabaseStartupOptions>() ?? new DatabaseStartupOptions();
databaseStartupOptions.Validate(builder.Environment);
builder.Services.AddSingleton(databaseStartupOptions);
builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection("Backup"));

var authenticationSessionOptions = builder.Configuration
    .GetSection(AuthenticationSessionOptions.SectionName)
    .Get<AuthenticationSessionOptions>() ?? new AuthenticationSessionOptions();
authenticationSessionOptions.Validate(builder.Environment);
builder.Services.AddSingleton(authenticationSessionOptions);
builder.Services.AddSingleton(new AuthenticationSessionCookies(
    authenticationSessionOptions,
    builder.Configuration));

var authorizationAttachmentOptions = builder.Configuration
    .GetSection(AuthorizationAttachmentOptions.SectionName)
    .Get<AuthorizationAttachmentOptions>() ?? new AuthorizationAttachmentOptions();
authorizationAttachmentOptions.Validate();
builder.Services.AddSingleton(authorizationAttachmentOptions);
builder.Services.AddSingleton<AuthorizationAttachmentStore>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = checked(
        authorizationAttachmentOptions.MaximumSizeBytes + 1024 * 1024);
});

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
builder.Services.AddScoped<FiscalProfileService>();
builder.Services.AddScoped<IdempotencyService>();
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
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

var authenticationRateLimits = builder.Configuration
    .GetSection(AuthenticationRateLimitOptions.SectionName)
    .Get<AuthenticationRateLimitOptions>() ?? new AuthenticationRateLimitOptions();
authenticationRateLimits.Validate();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problem = new ProblemDetails
        {
            Type = "https://httpstatuses.com/429",
            Title = "Demasiadas solicitudes",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = "Espere antes de volver a intentar esta operación.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers.RetryAfter =
                retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    };

    options.AddPolicy(AuthenticationRateLimitPolicyNames.Login, context =>
        CreateFixedWindowPartition(
            GetRateLimitClientKey(context),
            authenticationRateLimits.LoginPermitLimit,
            authenticationRateLimits.LoginWindowSeconds));
    options.AddPolicy(AuthenticationRateLimitPolicyNames.Refresh, context =>
        CreateFixedWindowPartition(
            GetRateLimitClientKey(context),
            authenticationRateLimits.RefreshPermitLimit,
            authenticationRateLimits.RefreshWindowSeconds));
    options.AddPolicy(AuthenticationRateLimitPolicyNames.ChangePassword, context =>
        CreateFixedWindowPartition(
            GetRateLimitClientKey(context),
            authenticationRateLimits.ChangePasswordPermitLimit,
            authenticationRateLimits.ChangePasswordWindowSeconds));
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt__SecretKey con un secreto de al menos 32 bytes fuera del repositorio.");
}

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
        ClockSkew = TimeSpan.FromSeconds(30)
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
        },
        OnTokenValidated = async context =>
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var securityVersionValue = context.Principal?.FindFirstValue(SecurityClaimTypes.SecurityVersion);
            if (!Guid.TryParse(userIdValue, out var userId)
                || !int.TryParse(securityVersionValue, out var securityVersion))
            {
                context.Fail("Token sin versión de seguridad válida.");
                return;
            }

            var database = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var userState = await database.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new { user.IsActive, user.IsDeleted, user.SecurityVersion })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            if (userState is null
                || !userState.IsActive
                || userState.IsDeleted
                || userState.SecurityVersion != securityVersion)
            {
                context.Fail("La sesión fue revocada.");
            }
        }
    };
});

builder.Services.AddHotelAuthorization();

// CORS
var corsOrigins = GetValidatedCorsOrigins(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    var mustChangePassword = bool.TryParse(
        context.User.FindFirstValue(SecurityClaimTypes.MustChangePassword),
        out var required) && required;
    var passwordChangeAllowedPaths = new[]
    {
        new PathString("/api/auth/change-password"),
        new PathString("/api/auth/logout"),
        new PathString("/api/auth/me")
    };
    var canContinueBeforePasswordChange = passwordChangeAllowedPaths
        .Any(path => context.Request.Path.Equals(path));

    if (context.User.Identity?.IsAuthenticated == true
        && mustChangePassword
        && !canContinueBeforePasswordChange)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/403",
            title = "Cambio de contraseña requerido",
            status = StatusCodes.Status403Forbidden,
            detail = "Cambie la contraseña inicial antes de utilizar el sistema."
        }, context.RequestAborted);
        return;
    }

    await next();
});
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

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

    // Apply pending migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (databaseStartupOptions.ApplyMigrationsOnStartup)
        {
            context.Database.Migrate();
            Log.Information("Migrations applied successfully");
        }
        else
        {
            var pendingMigrations = context.Database.GetPendingMigrations().ToArray();
            if (pendingMigrations.Length > 0)
            {
                throw new InvalidOperationException(
                    $"La base tiene {pendingMigrations.Length} migración(es) pendiente(s). " +
                    "Ejecútelas con la credencial de migración antes de iniciar la API.");
            }

            Log.Information("Database schema version verified; runtime migrations are disabled");
        }

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
                new() { AccountNumber = "2105", AccountName = "Saldos a favor de clientes", AccountType = AccountType.Pasivo, IsActive = true },
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
                { "2101", "21" }, { "2102", "21" }, { "2103", "21" }, { "2104", "21" }, { "2105", "21" },
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

        await SecurityCatalogSeeder.SeedAsync(context, builder.Configuration, app.Logger);
        Log.Information("Security roles and permissions synchronized");

        if (!context.BusinessSettings.Any())
        {
            context.BusinessSettings.Add(new BusinessSettings());
            context.SaveChanges();
            Log.Warning("Se creó un perfil fiscal vacío en estado Borrador; la emisión permanece bloqueada hasta su aprobación.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static string GetRateLimitClientKey(HttpContext context)
{
    var authenticatedUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!string.IsNullOrWhiteSpace(authenticatedUserId))
    {
        return $"user:{authenticatedUserId}";
    }

    var remoteIpAddress = context.Connection.RemoteIpAddress;
    if (remoteIpAddress?.IsIPv4MappedToIPv6 == true)
    {
        remoteIpAddress = remoteIpAddress.MapToIPv4();
    }

    return $"ip:{remoteIpAddress?.ToString() ?? "unknown"}";
}

static string[] GetValidatedCorsOrigins(IConfiguration configuration)
{
    var origins = (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (origins.Length == 0)
    {
        throw new InvalidOperationException("Configure al menos un origen en Cors__AllowedOrigins.");
    }

    foreach (var origin in origins)
    {
        if (origin.Contains('*', StringComparison.Ordinal)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException(
                $"El origen CORS '{origin}' no es un origen HTTP/HTTPS válido sin ruta, comodín ni credenciales.");
        }
    }

    return origins;
}

static RateLimitPartition<string> CreateFixedWindowPartition(
    string partitionKey,
    int permitLimit,
    int windowSeconds)
    => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

public partial class Program;
