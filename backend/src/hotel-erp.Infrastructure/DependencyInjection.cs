using hotel_erp.Application.Interfaces;
using hotel_erp.Infrastructure.Persistence;
using hotel_erp.Infrastructure.Persistence.Repositories;
using hotel_erp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace hotel_erp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<IGuestRepository, GuestRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IFolioRepository, FolioRepository>();
            services.AddScoped<ICAIRepository, CAIRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
            services.AddScoped<ICashMovementRepository, CashMovementRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            // Infrastructure services
            services.AddScoped<AuditService>();
            services.AddScoped<IBusinessSettingsRepository, BusinessSettingsRepository>();
            services.AddScoped<IDiscountRepository, DiscountRepository>();
            services.AddScoped<EscPosService>();

            return services;
        }
    }
}
