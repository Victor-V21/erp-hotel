using System.Security.Claims;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace hotel_erp.UnitTests;

public class AuthorizationPolicyTests
{
    [Fact]
    public async Task PermissionPolicy_AllowsMatchingPermission()
    {
        var authorization = CreateAuthorizationService();
        var principal = CreatePrincipal(PermissionNames.ManageUsers);

        var result = await authorization.AuthorizeAsync(principal, null, PermissionNames.ManageUsers);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task PermissionPolicy_DeniesMissingPermission()
    {
        var authorization = CreateAuthorizationService();
        var principal = CreatePrincipal(PermissionNames.ViewReports);

        var result = await authorization.AuthorizeAsync(principal, null, PermissionNames.ManageUsers);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task PermissionPolicy_DeniesBootstrapUserUntilPasswordChanges()
    {
        var authorization = CreateAuthorizationService();
        var principal = CreatePrincipal(
            PermissionNames.ManageUsers,
            new Claim(SecurityClaimTypes.MustChangePassword, bool.TrueString));

        var result = await authorization.AuthorizeAsync(principal, null, PermissionNames.ManageUsers);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void RoleNameNormalization_IsCaseAndWhitespaceInvariant()
    {
        Assert.Equal("RECEPCIÓN", SecurityCatalogSeeder.NormalizeRoleName("  recepción "));
    }

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHotelAuthorization();
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal CreatePrincipal(string permission, params Claim[] extraClaims)
    {
        var claims = new List<Claim> { new(SecurityClaimTypes.Permission, permission) };
        claims.AddRange(extraClaims);
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
