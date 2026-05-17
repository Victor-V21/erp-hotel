using hotel_erp.Application.Interfaces;
using hotel_erp.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace hotel_erp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<TaxService>();

            return services;
        }
    }
}
