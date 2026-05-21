using System.Text;
using System.Reflection;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Repositories;
using hotel_erp.Api.Services;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// DbContext
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

builder.Services.AddControllers();
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
        ClockSkew = TimeSpan.FromMinutes(1)
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

try
{
    Log.Information("Starting Hotel ERP API");

    // Ensure upload directories exist
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Uploads", "authorizations"));

    // Apply pending migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Log.Information("Migrations applied successfully");
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
