using System.Net;
using System.Net.Http.Json;
using hotel_erp.Api.Dtos.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace hotel_erp.IntegrationTests;

public class DatabaseLeastPrivilegeTests
{
    [PostgreSqlFact]
    public async Task ApplicationRole_RunsOrdinaryFlow_ButCannotMigrateOrRewriteAudit()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var adminConnection = Environment.GetEnvironmentVariable("HOTEL_ERP_TEST_ADMIN_CONNECTION")!;
        await using var applicationRole = new TemporaryPostgreSqlRole(adminConnection);
        await using var database = new TemporaryPostgreSqlDatabase(adminConnection);
        await database.CreateAsync();
        using var environment = new EnvironmentVariableScope(new Dictionary<string, string?>
        {
            ["ConnectionStrings__DefaultConnection"] = database.ConnectionString,
            ["Jwt__SecretKey"] = "integration-only-secret-key-with-at-least-32-bytes",
            ["Jwt__Issuer"] = "hotel-erp-integration",
            ["Jwt__Audience"] = "hotel-erp-integration",
            ["Cors__AllowedOrigins__0"] = "http://localhost",
            ["Database__ApplyMigrationsOnStartup"] = "true",
            ["BootstrapAdmin__Username"] = "integration_admin",
            ["BootstrapAdmin__Email"] = "integration_admin@example.invalid",
            ["BootstrapAdmin__Password"] = "Integration!234"
        });

        await using (var migrationFactory = new HotelErpWebApplicationFactory(database.ConnectionString))
        using (var migrationClient = migrationFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        }))
        {
            Assert.Equal(HttpStatusCode.OK, (await migrationClient.GetAsync("/health")).StatusCode);
        }

        await applicationRole.CreateAsync();
        await GrantApplicationPrivilegesAsync(
            database.ConnectionString,
            applicationRole.Name);

        var runtimeConnection = applicationRole.BuildConnectionString(database.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", runtimeConnection);
        Environment.SetEnvironmentVariable("Database__ApplyMigrationsOnStartup", "false");
        await using var runtimeFactory = new HotelErpWebApplicationFactory(
            runtimeConnection,
            configureSettings: new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "false"
            });
        using var client = runtimeFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "integration_admin",
            password = "Integration!234"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotNull(session.AccessToken);

        await using var restrictedConnection = new NpgsqlConnection(runtimeConnection);
        await restrictedConnection.OpenAsync();
        await AssertRoleFlagsAsync(restrictedConnection);
        Assert.True(await ScalarAsync<bool>(
            restrictedConnection,
            "SELECT has_database_privilege(current_user, current_database(), 'CONNECT')"));
        Assert.False(await ScalarAsync<bool>(
            restrictedConnection,
            "SELECT has_database_privilege(current_user, current_database(), 'CREATE')"));
        Assert.False(await ScalarAsync<bool>(
            restrictedConnection,
            "SELECT has_database_privilege(current_user, current_database(), 'TEMP')"));
        Assert.True(await ScalarAsync<long>(
            restrictedConnection,
            "SELECT COUNT(*) FROM \"AuditLogs\"") >= 1);

        await AssertInsufficientPrivilegeAsync(
            restrictedConnection,
            "CREATE TABLE public.\"UnauthorizedSchemaChange\" (\"Id\" integer)");
        await AssertInsufficientPrivilegeAsync(
            restrictedConnection,
            "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('forged', '0')");
        await AssertInsufficientPrivilegeAsync(
            restrictedConnection,
            "UPDATE \"AuditLogs\" SET \"Action\" = \"Action\"");
        await AssertInsufficientPrivilegeAsync(
            restrictedConnection,
            "DELETE FROM \"AuditLogs\"");
        await AssertInsufficientPrivilegeAsync(
            restrictedConnection,
            "SELECT rolpassword FROM pg_authid");
    }

    private static async Task GrantApplicationPrivilegesAsync(
        string databaseConnectionString,
        string roleName)
    {
        var databaseName = new NpgsqlConnectionStringBuilder(databaseConnectionString).Database!;
        await using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            REVOKE ALL PRIVILEGES ON DATABASE "{databaseName}" FROM PUBLIC;
            GRANT CONNECT ON DATABASE "{databaseName}" TO "{roleName}";
            REVOKE CREATE ON SCHEMA public FROM PUBLIC;
            GRANT USAGE ON SCHEMA public TO "{roleName}";
            REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM "{roleName}";
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "{roleName}";
            REVOKE UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER ON TABLE "AuditLogs" FROM "{roleName}";
            REVOKE ALL PRIVILEGES ON TABLE "__EFMigrationsHistory" FROM "{roleName}";
            GRANT SELECT ON TABLE "__EFMigrationsHistory" TO "{roleName}";
            REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM "{roleName}";
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "{roleName}";
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertRoleFlagsAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT rolsuper, rolcreatedb, rolcreaterole, rolinherit, rolreplication, rolbypassrls
            FROM pg_roles
            WHERE rolname = current_user
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.False(reader.GetBoolean(0));
        Assert.False(reader.GetBoolean(1));
        Assert.False(reader.GetBoolean(2));
        Assert.False(reader.GetBoolean(3));
        Assert.False(reader.GetBoolean(4));
        Assert.False(reader.GetBoolean(5));
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task AssertInsufficientPrivilegeAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }
}
