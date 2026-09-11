namespace hotel_erp.Api.Database;

public sealed class DatabaseStartupOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsOnStartup { get; init; }

    public void Validate(IHostEnvironment environment)
    {
        if (ApplyMigrationsOnStartup && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Database__ApplyMigrationsOnStartup solo puede activarse en Development.");
        }
    }
}
