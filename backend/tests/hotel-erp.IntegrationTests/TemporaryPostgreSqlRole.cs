using Npgsql;

namespace hotel_erp.IntegrationTests;

public sealed class TemporaryPostgreSqlRole : IAsyncDisposable
{
    private readonly string _adminConnectionString;

    public TemporaryPostgreSqlRole(string adminConnectionString)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        _adminConnectionString = adminBuilder.ConnectionString;
        Name = $"hotel_erp_app_it_{Guid.NewGuid():N}";
        Password = $"QaRole{Guid.NewGuid():N}!";
    }

    public string Name { get; }
    public string Password { get; }

    public async Task CreateAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE ROLE "{Name}"
            LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS
            PASSWORD '{Password}'
            """;
        await command.ExecuteNonQueryAsync();
    }

    public string BuildConnectionString(string databaseConnectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(databaseConnectionString)
        {
            Username = Name,
            Password = Password,
            Pooling = false
        };
        return builder.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP ROLE IF EXISTS \"{Name}\"";
        await command.ExecuteNonQueryAsync();
    }
}
