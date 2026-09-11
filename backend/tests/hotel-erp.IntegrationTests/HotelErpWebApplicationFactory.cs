using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace hotel_erp.IntegrationTests;

public sealed class HotelErpWebApplicationFactory(
    string connectionString,
    Action<IServiceCollection>? configureServices = null,
    IReadOnlyDictionary<string, string?>? configureSettings = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:SecretKey"] = "integration-only-secret-key-with-at-least-32-bytes",
                ["Jwt:Issuer"] = "hotel-erp-integration",
                ["Jwt:Audience"] = "hotel-erp-integration",
                ["Cors:AllowedOrigins:0"] = "http://localhost",
                ["BootstrapAdmin:Username"] = "integration_admin",
                ["BootstrapAdmin:Email"] = "integration_admin@example.invalid",
                ["BootstrapAdmin:Password"] = "Integration!234"
            });
            if (configureSettings is not null)
            {
                configuration.AddInMemoryCollection(configureSettings);
            }
        });

        if (configureServices is not null)
        {
            builder.ConfigureServices(configureServices);
        }
    }
}
