using Xunit;

namespace hotel_erp.IntegrationTests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOTEL_ERP_TEST_ADMIN_CONNECTION")))
        {
            Skip = "Configure HOTEL_ERP_TEST_ADMIN_CONNECTION para ejecutar integración sobre PostgreSQL.";
        }
    }
}
