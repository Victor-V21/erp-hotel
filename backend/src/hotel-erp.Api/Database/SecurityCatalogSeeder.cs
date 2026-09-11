using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Database;

public static class SecurityCatalogSeeder
{
    private static readonly string[] ReceptionPermissionNames =
    [
        PermissionNames.CreateInvoices,
        PermissionNames.ManageReservations,
        PermissionNames.ManageCash
    ];

    private static readonly string[] CashierPermissionNames =
    [
        PermissionNames.ManageCash
    ];

    private static readonly string[] AccountantPermissionNames =
    [
        PermissionNames.ManageTaxes,
        PermissionNames.ManageAccounting,
        PermissionNames.ViewReports,
        PermissionNames.ExportData,
        PermissionNames.ViewAudit
    ];

    private static readonly IReadOnlyDictionary<string, string> PermissionDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PermissionNames.ManageUsers] = "Gestionar usuarios",
            [PermissionNames.ManageRoles] = "Gestionar roles y permisos",
            [PermissionNames.ManageSettings] = "Gestionar configuración general",
            [PermissionNames.ManageTaxes] = "Gestionar configuración y autorizaciones fiscales",
            [PermissionNames.ManageBackups] = "Crear, descargar y revisar respaldos",
            [PermissionNames.ViewAudit] = "Consultar auditoría e integridad",
            [PermissionNames.ExportData] = "Exportar datos operativos y personales",
            [PermissionNames.ManageAccounting] = "Gestionar contabilidad",
            [PermissionNames.ViewReports] = "Consultar reportes",
            [PermissionNames.CreateInvoices] = "Crear documentos fiscales",
            [PermissionNames.ManageReservations] = "Gestionar reservaciones",
            [PermissionNames.ManageCash] = "Gestionar caja",
            [PermissionNames.ManageInventory] = "Gestionar inventario"
        };

    public static async Task SeedAsync(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var permissionsByName = await context.Permissions
            .ToDictionaryAsync(permission => permission.Name, StringComparer.Ordinal, cancellationToken);

        foreach (var (name, description) in PermissionDescriptions)
        {
            if (permissionsByName.ContainsKey(name))
            {
                continue;
            }

            var permission = new Permission { Name = name, Description = description };
            context.Permissions.Add(permission);
            permissionsByName[name] = permission;
        }

        var roles = await context.Roles.ToListAsync(cancellationToken);
        var administrator = EnsureSystemRole(
            context,
            roles,
            SystemRoleKeys.Administrator,
            "Admin",
            "Administrador del sistema");
        var reception = EnsureSystemRole(
            context,
            roles,
            SystemRoleKeys.Reception,
            "Recepcion",
            "Personal de recepción");
        var cashier = EnsureSystemRole(
            context,
            roles,
            SystemRoleKeys.Cashier,
            "Caja",
            "Personal responsable de cobros y caja");
        var accountant = EnsureSystemRole(
            context,
            roles,
            SystemRoleKeys.Accountant,
            "Contador",
            "Personal responsable de contabilidad y revisión fiscal");

        foreach (var role in roles.Where(role => string.IsNullOrWhiteSpace(role.NormalizedName)))
        {
            role.NormalizedName = NormalizeRoleName(role.Name);
        }

        await context.SaveChangesAsync(cancellationToken);

        var assignedPermissionIds = await context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == administrator.Id)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToHashSetAsync(cancellationToken);

        foreach (var permission in permissionsByName.Values.Where(permission => !assignedPermissionIds.Contains(permission.Id)))
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = administrator.Id,
                PermissionId = permission.Id
            });
        }

        var receptionPermissionIds = await context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == reception.Id)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToHashSetAsync(cancellationToken);
        foreach (var permissionName in ReceptionPermissionNames)
        {
            var permission = permissionsByName[permissionName];
            if (!receptionPermissionIds.Contains(permission.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = reception.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await AddMissingPermissionsAsync(
            context,
            cashier,
            CashierPermissionNames.Select(permissionName => permissionsByName[permissionName]),
            cancellationToken);
        await AddMissingPermissionsAsync(
            context,
            accountant,
            AccountantPermissionNames.Select(permissionName => permissionsByName[permissionName]),
            cancellationToken);

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var username = configuration["BootstrapAdmin:Username"]?.Trim();
            var password = configuration["BootstrapAdmin:Password"];
            var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "La base no contiene usuarios. Configure BootstrapAdmin__Username, BootstrapAdmin__Email y BootstrapAdmin__Password para crear el administrador inicial.");
            }

            if (password.Length < 12)
            {
                throw new InvalidOperationException("BootstrapAdmin__Password debe contener al menos 12 caracteres.");
            }

            var user = new User
            {
                Username = username,
                Email = email,
                FirstName = configuration["BootstrapAdmin:FirstName"]?.Trim() ?? "Administrador",
                LastName = configuration["BootstrapAdmin:LastName"]?.Trim() ?? "Sistema",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                MustChangePassword = true
            };

            context.Users.Add(user);
            context.UserRoles.Add(new UserRole { User = user, Role = administrator });
            logger.LogWarning("Se creó el administrador inicial {Username}; debe cambiar su contraseña antes de operar.", username);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static string NormalizeRoleName(string name) => name.Trim().ToUpperInvariant();

    private static Role EnsureSystemRole(
        ApplicationDbContext context,
        List<Role> roles,
        string systemKey,
        string name,
        string description)
    {
        var normalizedName = NormalizeRoleName(name);
        var role = roles.FirstOrDefault(candidate => candidate.SystemKey == systemKey)
            ?? roles.FirstOrDefault(candidate => candidate.NormalizedName == normalizedName)
            ?? roles.FirstOrDefault(candidate => NormalizeRoleName(candidate.Name) == normalizedName);

        if (role is null)
        {
            role = new Role
            {
                Name = name,
                NormalizedName = normalizedName,
                SystemKey = systemKey,
                Description = description
            };
            context.Roles.Add(role);
            roles.Add(role);
        }
        else
        {
            role.NormalizedName = NormalizeRoleName(role.Name);
            role.SystemKey = systemKey;
            role.Description ??= description;
        }

        return role;
    }

    private static async Task AddMissingPermissionsAsync(
        ApplicationDbContext context,
        Role role,
        IEnumerable<Permission> permissions,
        CancellationToken cancellationToken)
    {
        var assignedPermissionIds = await context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == role.Id)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToHashSetAsync(cancellationToken);

        foreach (var permission in permissions.Where(permission => !assignedPermissionIds.Contains(permission.Id)))
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }
    }
}
