namespace hotel_erp.Api.Services;

public sealed class AuthenticationRateLimitOptions
{
    public const string SectionName = "AuthenticationRateLimiting";

    public int LoginPermitLimit { get; init; } = 10;
    public int LoginWindowSeconds { get; init; } = 60;
    public int RefreshPermitLimit { get; init; } = 30;
    public int RefreshWindowSeconds { get; init; } = 60;
    public int ChangePasswordPermitLimit { get; init; } = 5;
    public int ChangePasswordWindowSeconds { get; init; } = 300;

    public void Validate()
    {
        ValidatePositive(LoginPermitLimit, nameof(LoginPermitLimit));
        ValidatePositive(LoginWindowSeconds, nameof(LoginWindowSeconds));
        ValidatePositive(RefreshPermitLimit, nameof(RefreshPermitLimit));
        ValidatePositive(RefreshWindowSeconds, nameof(RefreshWindowSeconds));
        ValidatePositive(ChangePasswordPermitLimit, nameof(ChangePasswordPermitLimit));
        ValidatePositive(ChangePasswordWindowSeconds, nameof(ChangePasswordWindowSeconds));
    }

    private static void ValidatePositive(int value, string propertyName)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{propertyName} debe ser un entero mayor que cero.");
        }
    }
}

