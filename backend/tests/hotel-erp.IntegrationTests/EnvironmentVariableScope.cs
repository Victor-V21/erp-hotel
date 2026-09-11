namespace hotel_erp.IntegrationTests;

public sealed class EnvironmentVariableScope : IDisposable
{
    private readonly Dictionary<string, string?> _previousValues;

    public EnvironmentVariableScope(IReadOnlyDictionary<string, string?> values)
    {
        _previousValues = values.Keys.ToDictionary(
            key => key,
            Environment.GetEnvironmentVariable,
            StringComparer.Ordinal);

        foreach (var (key, value) in values)
            Environment.SetEnvironmentVariable(key, value);
    }

    public void Dispose()
    {
        foreach (var (key, value) in _previousValues)
            Environment.SetEnvironmentVariable(key, value);
    }
}
