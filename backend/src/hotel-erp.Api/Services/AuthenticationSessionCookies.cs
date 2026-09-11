using System.Security.Cryptography;
using System.Text;

namespace hotel_erp.Api.Services;

public sealed class AuthenticationSessionOptions
{
    public const string SectionName = "AuthenticationSession";

    public string RefreshCookieName { get; init; } = "hotel_erp_refresh";
    public string CsrfCookieName { get; init; } = "hotel_erp_csrf";
    public string CsrfHeaderName { get; init; } = "X-CSRF-Token";
    public bool SecureCookies { get; init; } = true;

    public void Validate(IHostEnvironment environment)
    {
        ValidateTokenName(RefreshCookieName, nameof(RefreshCookieName));
        ValidateTokenName(CsrfCookieName, nameof(CsrfCookieName));
        ValidateTokenName(CsrfHeaderName, nameof(CsrfHeaderName));

        if (string.Equals(RefreshCookieName, CsrfCookieName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Las cookies de refresh y CSRF deben usar nombres distintos.");
        }

        if (!SecureCookies && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "AuthenticationSession__SecureCookies solo puede desactivarse en Development.");
        }
    }

    private static void ValidateTokenName(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Any(character => !IsTokenCharacter(character)))
        {
            throw new InvalidOperationException($"AuthenticationSession__{propertyName} no es válido.");
        }
    }

    private static bool IsTokenCharacter(char character)
        => char.IsAsciiLetterOrDigit(character)
            || character is '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-'
                or '.' or '^' or '_' or '`' or '|' or '~';
}

public sealed class AuthenticationSessionCookies
{
    private readonly AuthenticationSessionOptions _options;
    private readonly TimeSpan _refreshLifetime;

    public AuthenticationSessionCookies(
        AuthenticationSessionOptions options,
        IConfiguration configuration)
    {
        _options = options;
        var refreshDays = configuration.GetValue<int?>("Jwt:RefreshTokenExpirationInDays") ?? 7;
        if (refreshDays is < 1 or > 90)
        {
            throw new InvalidOperationException("Jwt__RefreshTokenExpirationInDays debe estar entre 1 y 90.");
        }

        _refreshLifetime = TimeSpan.FromDays(refreshDays);
    }

    public string? ReadRefreshToken(HttpRequest request)
        => request.Cookies.TryGetValue(_options.RefreshCookieName, out var token)
            ? token
            : null;

    public bool HasValidCsrfToken(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(_options.CsrfCookieName, out var cookieToken))
        {
            return false;
        }

        var headerToken = request.Headers[_options.CsrfHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(headerToken))
        {
            return false;
        }

        var cookieHash = SHA256.HashData(Encoding.UTF8.GetBytes(cookieToken));
        var headerHash = SHA256.HashData(Encoding.UTF8.GetBytes(headerToken));
        return CryptographicOperations.FixedTimeEquals(cookieHash, headerHash);
    }

    public void Issue(HttpResponse response, string refreshToken)
    {
        var csrfToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        response.Cookies.Append(
            _options.RefreshCookieName,
            refreshToken,
            CreateCookieOptions(httpOnly: true, path: "/api/auth"));
        response.Cookies.Append(
            _options.CsrfCookieName,
            csrfToken,
            CreateCookieOptions(httpOnly: false, path: "/"));
    }

    public void Clear(HttpResponse response)
    {
        response.Cookies.Delete(
            _options.RefreshCookieName,
            CreateDeletionOptions(httpOnly: true, path: "/api/auth"));
        response.Cookies.Delete(
            _options.CsrfCookieName,
            CreateDeletionOptions(httpOnly: false, path: "/"));
    }

    private CookieOptions CreateCookieOptions(bool httpOnly, string path) => new()
    {
        HttpOnly = httpOnly,
        Secure = _options.SecureCookies,
        SameSite = SameSiteMode.Strict,
        Path = path,
        MaxAge = _refreshLifetime,
        IsEssential = true
    };

    private CookieOptions CreateDeletionOptions(bool httpOnly, string path) => new()
    {
        HttpOnly = httpOnly,
        Secure = _options.SecureCookies,
        SameSite = SameSiteMode.Strict,
        Path = path,
        IsEssential = true
    };
}
