using Npgsql;

namespace hotel_erp.IntegrationTests;

public sealed class TemporaryPostgreSqlDatabase : IAsyncDisposable
{
    private readonly string _adminConnectionString;
    private readonly string _databaseName = $"hotel_erp_it_{Guid.NewGuid():N}";

    public TemporaryPostgreSqlDatabase(string adminConnectionString)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        _adminConnectionString = adminBuilder.ConnectionString;

        var testBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = _databaseName,
            Pooling = false
        };
        ConnectionString = testBuilder.ConnectionString;
    }

    public string ConnectionString { get; }

    public async Task CreateAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }
}
