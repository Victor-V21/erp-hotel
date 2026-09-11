using hotel_erp.Api.Authorization;
using hotel_erp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

namespace hotel_erp.UnitTests;

public class EndpointAuthorizationTests
{
    [Theory]
    [InlineData(typeof(RolesController), PermissionNames.ManageRoles)]
    [InlineData(typeof(UsersController), PermissionNames.ManageUsers)]
    [InlineData(typeof(BackupController), PermissionNames.ManageBackups)]
    [InlineData(typeof(AuditLogsController), PermissionNames.ViewAudit)]
    [InlineData(typeof(DataExportController), PermissionNames.ExportData)]
    [InlineData(typeof(ReportsController), PermissionNames.ViewReports)]
    [InlineData(typeof(AccountsController), PermissionNames.ManageAccounting)]
    [InlineData(typeof(CardSettlementsController), PermissionNames.ManageAccounting)]
    [InlineData(typeof(PurchaseInvoicesController), PermissionNames.ManageAccounting)]
    [InlineData(typeof(SuppliersController), PermissionNames.ManageAccounting)]
    [InlineData(typeof(RefundsController), PermissionNames.ManageCash)]
    [InlineData(typeof(InventoryController), PermissionNames.ManageInventory)]
    public void SensitiveController_RequiresExpectedPolicy(Type controllerType, string policy)
    {
        var policies = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .ToList();

        Assert.Contains(policy, policies);
    }

    [Theory]
    [InlineData(typeof(SettingsController), "Update", PermissionNames.ManageSettings)]
    [InlineData(typeof(TaxConfigurationsController), "Create", PermissionNames.ManageTaxes)]
    [InlineData(typeof(TaxConfigurationsController), "Update", PermissionNames.ManageTaxes)]
    [InlineData(typeof(TaxConfigurationsController), "Delete", PermissionNames.ManageTaxes)]
    [InlineData(typeof(CAIController), "Create", PermissionNames.ManageTaxes)]
    [InlineData(typeof(CAIController), "Delete", PermissionNames.ManageTaxes)]
    [InlineData(typeof(DocumentAuthorizationsController), "Create", PermissionNames.ManageTaxes)]
    [InlineData(typeof(DocumentAuthorizationsController), "CreateWithFile", PermissionNames.ManageTaxes)]
    [InlineData(typeof(DocumentAuthorizationsController), "GetFile", PermissionNames.ManageTaxes)]
    [InlineData(typeof(DocumentAuthorizationsController), "Delete", PermissionNames.ManageTaxes)]
    [InlineData(typeof(RoomsController), "Create", PermissionNames.ManageReservations)]
    [InlineData(typeof(RoomsController), "Update", PermissionNames.ManageReservations)]
    [InlineData(typeof(RoomsController), "Delete", PermissionNames.ManageReservations)]
    [InlineData(typeof(RoomTypesController), "Create", PermissionNames.ManageReservations)]
    [InlineData(typeof(RoomTypesController), "Update", PermissionNames.ManageReservations)]
    [InlineData(typeof(RoomTypesController), "Delete", PermissionNames.ManageReservations)]
    [InlineData(typeof(CustomersController), "Create", PermissionNames.ManageReservations)]
    [InlineData(typeof(CustomersController), "Update", PermissionNames.ManageReservations)]
    [InlineData(typeof(CustomersController), "Delete", PermissionNames.ManageReservations)]
    [InlineData(typeof(GuestsController), "Create", PermissionNames.ManageReservations)]
    [InlineData(typeof(GuestsController), "Update", PermissionNames.ManageReservations)]
    [InlineData(typeof(GuestsController), "UpdateClassification", PermissionNames.ManageReservations)]
    [InlineData(typeof(GuestsController), "Delete", PermissionNames.ManageReservations)]
    [InlineData(typeof(DiscountsController), "Create", PermissionNames.ManageSettings)]
    [InlineData(typeof(DiscountsController), "Update", PermissionNames.ManageSettings)]
    [InlineData(typeof(DiscountsController), "Delete", PermissionNames.ManageSettings)]
    [InlineData(typeof(PrintController), "GetPrinters", PermissionNames.ManageSettings)]
    [InlineData(typeof(PrintController), "TestPrinter", PermissionNames.ManageSettings)]
    [InlineData(typeof(PrintController), "GetTestPreview", PermissionNames.ManageSettings)]
    [InlineData(typeof(PrintController), "PrintTest", PermissionNames.ManageSettings)]
    [InlineData(typeof(PrintController), "GetTestRuler", PermissionNames.ManageSettings)]
    public void SensitiveAction_RequiresExpectedPolicy(Type controllerType, string actionName, string policy)
    {
        var method = controllerType.GetMethod(actionName);
        Assert.NotNull(method);

        var policies = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .ToList();

        Assert.Contains(policy, policies);
    }

    [Theory]
    [InlineData("Login", AuthenticationRateLimitPolicyNames.Login)]
    [InlineData("Refresh", AuthenticationRateLimitPolicyNames.Refresh)]
    [InlineData("ChangePassword", AuthenticationRateLimitPolicyNames.ChangePassword)]
    public void AuthenticationAction_RequiresExpectedRateLimitPolicy(string actionName, string policy)
    {
        var method = typeof(AuthController).GetMethod(actionName);
        Assert.NotNull(method);

        var rateLimitPolicies = method!
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .Select(attribute => attribute.PolicyName)
            .ToList();

        Assert.Contains(policy, rateLimitPolicies);
    }

    [Fact]
    public void EveryControllerAction_IsAuthorizedOrExplicitlyAnonymous()
    {
        var unsecuredActions = typeof(AuthController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controllerType => controllerType
                .GetMethods(System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Length > 0)
                .Select(method => new { ControllerType = controllerType, Method = method }))
            .Where(action =>
            {
                var explicitlyAnonymous = action.Method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                    || action.ControllerType.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
                var authorized = action.Method.IsDefined(typeof(AuthorizeAttribute), inherit: true)
                    || action.ControllerType.IsDefined(typeof(AuthorizeAttribute), inherit: true);
                return !explicitlyAnonymous && !authorized;
            })
            .Select(action => $"{action.ControllerType.Name}.{action.Method.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unsecuredActions.Count == 0,
            $"Acciones sin [Authorize] o [AllowAnonymous]: {string.Join(", ", unsecuredActions)}");
    }

    [Fact]
    public void EveryStateChangingAction_RequiresPermissionOrDocumentedSessionScope()
    {
        var sessionScopedActions = new HashSet<string>(StringComparer.Ordinal)
        {
            $"{nameof(AuthController)}.{nameof(AuthController.Logout)}",
            $"{nameof(AuthController)}.{nameof(AuthController.ChangePassword)}",
            $"{nameof(PrintController)}.{nameof(PrintController.PrintInvoice)}"
        };

        var actionsWithoutPermission = typeof(AuthController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controllerType => controllerType
                .GetMethods(System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true)
                    .SelectMany(attribute => attribute.HttpMethods)
                    .Any(httpMethod => httpMethod is "POST" or "PUT" or "PATCH" or "DELETE"))
                .Select(method => new { ControllerType = controllerType, Method = method }))
            .Where(action => !action.Method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
            .Where(action =>
            {
                var name = $"{action.ControllerType.Name}.{action.Method.Name}";
                if (sessionScopedActions.Contains(name))
                {
                    return false;
                }

                return action.Method
                    .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                    .Concat(action.ControllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
                    .All(attribute => string.IsNullOrWhiteSpace(attribute.Policy));
            })
            .Select(action => $"{action.ControllerType.Name}.{action.Method.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            actionsWithoutPermission.Count == 0,
            $"Mutaciones sin política de permiso: {string.Join(", ", actionsWithoutPermission)}");
    }
}
