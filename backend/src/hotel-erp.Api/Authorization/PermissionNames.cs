using Microsoft.AspNetCore.Authorization;

namespace hotel_erp.Api.Authorization;

public static class PermissionNames
{
    public const string ManageUsers = "manage_users";
    public const string ManageRoles = "manage_roles";
    public const string ManageSettings = "manage_settings";
    public const string ManageTaxes = "manage_taxes";
    public const string ManageBackups = "manage_backups";
    public const string ViewAudit = "view_audit";
    public const string ExportData = "export_data";
    public const string ManageAccounting = "manage_accounting";
    public const string ViewReports = "view_reports";
    public const string CreateInvoices = "create_invoices";
    public const string ManageReservations = "manage_reservations";
    public const string ManageCash = "manage_cash";
    public const string ManageInventory = "manage_inventory";

    public static readonly IReadOnlyList<string> All =
    [
        ManageUsers,
        ManageRoles,
        ManageSettings,
        ManageTaxes,
        ManageBackups,
        ViewAudit,
        ExportData,
        ManageAccounting,
        ViewReports,
        CreateInvoices,
        ManageReservations,
        ManageCash,
        ManageInventory
    ];
}

public static class SystemRoleKeys
{
    public const string Administrator = "administrator";
    public const string Reception = "reception";
    public const string Cashier = "cashier";
    public const string Accountant = "accountant";
}

public static class SecurityClaimTypes
{
    public const string Permission = "permission";
    public const string SecurityVersion = "security_version";
    public const string MustChangePassword = "must_change_password";
}

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddHotelAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionNames.All)
            {
                options.AddPolicy(permission, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(SecurityClaimTypes.Permission, permission)
                    .RequireAssertion(context =>
                        !string.Equals(
                            context.User.FindFirst(SecurityClaimTypes.MustChangePassword)?.Value,
                            bool.TrueString,
                            StringComparison.OrdinalIgnoreCase)));
            }
        });

        return services;
    }
}
