using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Dtos.Auth;
using hotel_erp.Api.Dtos.Cash;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Dtos.Payments;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace hotel_erp.IntegrationTests;

public class AuthenticationFlowTests
{
    [PostgreSqlFact]
    public async Task AuthenticationAbuse_IsRateLimited_AndInternalErrorsAreSafe()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var adminConnection = Environment.GetEnvironmentVariable("HOTEL_ERP_TEST_ADMIN_CONNECTION")!;
        await using var database = new TemporaryPostgreSqlDatabase(adminConnection);
        await database.CreateAsync();
        using var environment = new EnvironmentVariableScope(new Dictionary<string, string?>
        {
            ["ConnectionStrings__DefaultConnection"] = database.ConnectionString,
            ["Jwt__SecretKey"] = "integration-only-secret-key-with-at-least-32-bytes",
            ["Jwt__Issuer"] = "hotel-erp-integration",
            ["Jwt__Audience"] = "hotel-erp-integration",
            ["Cors__AllowedOrigins__0"] = "http://localhost",
            ["BootstrapAdmin__Username"] = "integration_admin",
            ["BootstrapAdmin__Email"] = "integration_admin@example.invalid",
            ["BootstrapAdmin__Password"] = "Integration!234",
            ["AuthenticationRateLimiting__LoginPermitLimit"] = "2",
            ["AuthenticationRateLimiting__LoginWindowSeconds"] = "5"
        });

        await using (var rateLimitFactory = new HotelErpWebApplicationFactory(
            database.ConnectionString))
        using (var client = rateLimitFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        }))
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var rejectedCredentials = await client.PostAsJsonAsync("/api/auth/login", new
                {
                    username = "unknown_operator",
                    password = "Incorrect!234"
                });
                Assert.Equal(HttpStatusCode.Unauthorized, rejectedCredentials.StatusCode);
                await AssertSafeProblemDetailsAsync(
                    rejectedCredentials,
                    HttpStatusCode.Unauthorized,
                    "Autenticación rechazada");
            }

            using var limited = await client.PostAsJsonAsync("/api/auth/login", new
            {
                username = "another_unknown_operator",
                password = "Incorrect!567"
            });
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.True(limited.Headers.TryGetValues("Retry-After", out var retryAfterValues));
            Assert.All(retryAfterValues, value => Assert.True(int.TryParse(value, out var seconds) && seconds >= 1));
            await AssertSafeProblemDetailsAsync(
                limited,
                HttpStatusCode.TooManyRequests,
                "Demasiadas solicitudes");

            await Task.Delay(TimeSpan.FromMilliseconds(5200));
            using var recoveredLogin = await client.PostAsJsonAsync("/api/auth/login", new
            {
                username = "integration_admin",
                password = "Integration!234"
            });
            Assert.Equal(HttpStatusCode.OK, recoveredLogin.StatusCode);
            Assert.Equal("no-store", recoveredLogin.Headers.CacheControl?.ToString());
            var recoveredSession = (await recoveredLogin.Content.ReadFromJsonAsync<AuthResponse>())!;
            Assert.True(recoveredSession.Success);
            Assert.Null(recoveredSession.RefreshToken);

            var refreshCookieHeader = GetSetCookieHeader(recoveredLogin, "hotel_erp_refresh");
            var csrfCookieHeader = GetSetCookieHeader(recoveredLogin, "hotel_erp_csrf");
            Assert.Contains("httponly", refreshCookieHeader, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", refreshCookieHeader, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("httponly", csrfCookieHeader, StringComparison.OrdinalIgnoreCase);
            var csrfToken = GetSetCookieValue(recoveredLogin, "hotel_erp_csrf");

            using var missingCsrf = await client.PostAsJsonAsync("/api/auth/refresh", new { });
            Assert.Equal(HttpStatusCode.Forbidden, missingCsrf.StatusCode);

            using var cookieRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
            {
                Content = JsonContent.Create(new { })
            };
            cookieRefreshRequest.Headers.Add("X-CSRF-Token", csrfToken);
            using var cookieRefresh = await client.SendAsync(cookieRefreshRequest);
            Assert.Equal(HttpStatusCode.OK, cookieRefresh.StatusCode);
            var refreshedSession = (await cookieRefresh.Content.ReadFromJsonAsync<AuthResponse>())!;
            Assert.True(refreshedSession.Success);
            Assert.Null(refreshedSession.RefreshToken);
            Assert.NotEqual(csrfToken, GetSetCookieValue(cookieRefresh, "hotel_erp_csrf"));

            await AssertCorsSessionPolicyAsync(client);
        }

        await using var exceptionFactory = new HotelErpWebApplicationFactory(
            database.ConnectionString,
            configureServices: services => services.AddScoped<IAuthService, ThrowingAuthService>());
        using var exceptionClient = exceptionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var internalError = await exceptionClient.PostAsJsonAsync("/api/auth/login", new
        {
            username = "integration_admin",
            password = "Integration!234"
        });
        Assert.Equal(HttpStatusCode.InternalServerError, internalError.StatusCode);
        await AssertSafeProblemDetailsAsync(internalError, HttpStatusCode.InternalServerError);
    }

    [PostgreSqlFact]
    public async Task SystemRoleMatrix_EnforcesSensitiveEndpointPolicies()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var adminConnection = Environment.GetEnvironmentVariable("HOTEL_ERP_TEST_ADMIN_CONNECTION")!;
        await using var database = new TemporaryPostgreSqlDatabase(adminConnection);
        await database.CreateAsync();
        using var environment = new EnvironmentVariableScope(new Dictionary<string, string?>
        {
            ["ConnectionStrings__DefaultConnection"] = database.ConnectionString,
            ["Jwt__SecretKey"] = "integration-only-secret-key-with-at-least-32-bytes",
            ["Jwt__Issuer"] = "hotel-erp-integration",
            ["Jwt__Audience"] = "hotel-erp-integration",
            ["Cors__AllowedOrigins__0"] = "http://localhost",
            ["BootstrapAdmin__Username"] = "integration_admin",
            ["BootstrapAdmin__Email"] = "integration_admin@example.invalid",
            ["BootstrapAdmin__Password"] = "Integration!234"
        });
        await using var factory = new HotelErpWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        var anonymouslyCheckedActions = await AssertAnonymousControllerMatrixAsync(factory.Services, client);
        Assert.True(anonymouslyCheckedActions >= 60, $"Solo se inventariaron {anonymouslyCheckedActions} acciones HTTP.");
        await SeedRoleMatrixUsersAsync(factory.Services);

        var administrator = await LoginAsync(client, "Matrix!23456", "matrix_admin");
        var reception = await LoginAsync(client, "Matrix!23456", "matrix_reception");
        var cashier = await LoginAsync(client, "Matrix!23456", "matrix_cashier");
        var accountant = await LoginAsync(client, "Matrix!23456", "matrix_accountant");

        Assert.Equal(PermissionNames.All.Order(), administrator.User!.Permissions.Order());
        Assert.Equal(
            new[] { PermissionNames.CreateInvoices, PermissionNames.ManageCash, PermissionNames.ManageReservations }.Order(),
            reception.User!.Permissions.Order());
        Assert.Equal(new[] { PermissionNames.ManageCash }, cashier.User!.Permissions);
        Assert.Equal(
            new[]
            {
                PermissionNames.ExportData,
                PermissionNames.ManageAccounting,
                PermissionNames.ManageTaxes,
                PermissionNames.ViewAudit,
                PermissionNames.ViewReports
            }.Order(),
            accountant.User!.Permissions.Order());

        await AssertEndpointStatusAsync(client, null, "/api/cash-registers", HttpStatusCode.Unauthorized);
        await AssertEndpointStatusAsync(client, null, "/api/accounts/flat", HttpStatusCode.Unauthorized);
        await AssertEndpointStatusAsync(client, null, "/api/users", HttpStatusCode.Unauthorized);

        await AssertEndpointStatusAsync(client, reception.AccessToken, "/api/cash-registers", HttpStatusCode.OK);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, "/api/cash-registers", HttpStatusCode.OK);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, "/api/cash-registers", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, "/api/cash-registers", HttpStatusCode.OK);

        await AssertEndpointStatusAsync(client, reception.AccessToken, "/api/accounts/flat", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, "/api/accounts/flat", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, "/api/accounts/flat", HttpStatusCode.OK);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, "/api/accounts/flat", HttpStatusCode.OK);

        var missingAuthorizationFile = $"/api/document-authorizations/{Guid.NewGuid()}/file";
        await AssertEndpointStatusAsync(client, reception.AccessToken, missingAuthorizationFile, HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, missingAuthorizationFile, HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, missingAuthorizationFile, HttpStatusCode.NotFound);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, missingAuthorizationFile, HttpStatusCode.NotFound);

        await AssertEndpointStatusAsync(client, reception.AccessToken, "/api/users", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, "/api/users", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, "/api/users", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, "/api/users", HttpStatusCode.OK);

        await AssertEndpointStatusAsync(client, reception.AccessToken, "/api/backup/logs", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, "/api/backup/logs", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, "/api/backup/logs", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, "/api/backup/logs", HttpStatusCode.OK);

        await AssertEndpointStatusAsync(client, reception.AccessToken, "/api/audit-logs?limit=1", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, "/api/audit-logs?limit=1", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, "/api/audit-logs?limit=1", HttpStatusCode.OK);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, "/api/audit-logs?limit=1", HttpStatusCode.OK);

        await AssertEndpointStatusAsync(client, reception.AccessToken, "/api/inventory/categories", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, "/api/inventory/categories", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, "/api/inventory/categories", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, "/api/inventory/categories", HttpStatusCode.OK);

        await AssertEndpointStatusAsync(client, reception.AccessToken, "/api/suppliers", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, cashier.AccessToken, "/api/suppliers", HttpStatusCode.Forbidden);
        await AssertEndpointStatusAsync(client, accountant.AccessToken, "/api/suppliers", HttpStatusCode.OK);
        await AssertEndpointStatusAsync(client, administrator.AccessToken, "/api/suppliers", HttpStatusCode.OK);

        await AssertJsonEndpointStatusAsync(client, reception.AccessToken, "/api/rooms", HttpStatusCode.BadRequest);
        await AssertJsonEndpointStatusAsync(client, cashier.AccessToken, "/api/rooms", HttpStatusCode.Forbidden);
        await AssertJsonEndpointStatusAsync(client, accountant.AccessToken, "/api/rooms", HttpStatusCode.Forbidden);
        await AssertJsonEndpointStatusAsync(client, administrator.AccessToken, "/api/rooms", HttpStatusCode.BadRequest);

        await AssertJsonEndpointStatusAsync(client, reception.AccessToken, "/api/discounts", HttpStatusCode.Forbidden);
        await AssertJsonEndpointStatusAsync(client, cashier.AccessToken, "/api/discounts", HttpStatusCode.Forbidden);
        await AssertJsonEndpointStatusAsync(client, accountant.AccessToken, "/api/discounts", HttpStatusCode.Forbidden);
        await AssertJsonEndpointStatusAsync(client, administrator.AccessToken, "/api/discounts", HttpStatusCode.BadRequest);
    }

    [PostgreSqlFact]
    public async Task BootstrapPasswordChangeAndLogout_InvalidatePreviousTokens()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var adminConnection = Environment.GetEnvironmentVariable("HOTEL_ERP_TEST_ADMIN_CONNECTION")!;
        var attachmentBaseDirectory = Path.Combine(
            Path.GetTempPath(),
            $"hotel-erp-it-attachments-{Guid.NewGuid():N}");
        var attachmentDirectory = Path.Combine(attachmentBaseDirectory, "authorizations");
        await using var database = new TemporaryPostgreSqlDatabase(adminConnection);
        await database.CreateAsync();
        using var environment = new EnvironmentVariableScope(new Dictionary<string, string?>
        {
            ["ConnectionStrings__DefaultConnection"] = database.ConnectionString,
            ["Jwt__SecretKey"] = "integration-only-secret-key-with-at-least-32-bytes",
            ["Jwt__Issuer"] = "hotel-erp-integration",
            ["Jwt__Audience"] = "hotel-erp-integration",
            ["Cors__AllowedOrigins__0"] = "http://localhost",
            ["BootstrapAdmin__Username"] = "integration_admin",
            ["BootstrapAdmin__Email"] = "integration_admin@example.invalid",
            ["BootstrapAdmin__Password"] = "Integration!234",
            ["AuthorizationAttachments__Directory"] = attachmentDirectory
        });
        await using var factory = new HotelErpWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var anonymousUsers = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousUsers.StatusCode);

        var initialSession = await LoginAsync(client, "Integration!234");
        Assert.True(initialSession.User!.MustChangePassword);

        using var initialRoomsRequest = Authorized(HttpMethod.Get, "/api/rooms", initialSession.AccessToken!);
        var roomsBeforeChange = await client.SendAsync(initialRoomsRequest);
        Assert.Equal(HttpStatusCode.Forbidden, roomsBeforeChange.StatusCode);

        using var changeRequest = Authorized(HttpMethod.Post, "/api/auth/change-password", initialSession.AccessToken!);
        changeRequest.Content = JsonContent.Create(new
        {
            currentPassword = "Integration!234",
            newPassword = "Integration!567"
        });
        var changeResponse = await client.SendAsync(changeRequest);
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        using var revokedRequest = Authorized(HttpMethod.Get, "/api/rooms", initialSession.AccessToken!);
        var revokedResponse = await client.SendAsync(revokedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);

        var currentSession = await LoginAsync(client, "Integration!567");
        Assert.False(currentSession.User!.MustChangePassword);

        await AssertAuthorizationAttachmentSecurityAsync(
            client,
            factory.Services,
            currentSession.AccessToken!,
            attachmentBaseDirectory,
            attachmentDirectory);

        using var settingsRequest = Authorized(HttpMethod.Get, "/api/settings/business", currentSession.AccessToken!);
        var settingsResponse = await client.SendAsync(settingsRequest);
        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);
        var settings = (await settingsResponse.Content.ReadFromJsonAsync<BusinessSettingsDto>())!;
        Assert.Equal(FiscalProfileStatus.Borrador.ToString(), settings.FiscalProfileStatus);
        Assert.Empty(settings.RTN);

        using var missingIdempotencyKeyRequest = InvoiceRequest(currentSession.AccessToken!);
        var missingIdempotencyKeyResponse = await client.SendAsync(missingIdempotencyKeyRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingIdempotencyKeyResponse.StatusCode);

        using var invoiceRequest = InvoiceRequest(currentSession.AccessToken!, idempotencyKey: Guid.NewGuid().ToString("D"));
        var invoiceResponse = await client.SendAsync(invoiceRequest);
        Assert.Equal(HttpStatusCode.Conflict, invoiceResponse.StatusCode);

        using var invalidApprovalRequest = Authorized(HttpMethod.Post, "/api/settings/business/approve", currentSession.AccessToken!);
        invalidApprovalRequest.Content = JsonContent.Create(new
        {
            validFrom = HondurasToday(),
            approvalNote = "Validación incompleta de integración"
        });
        var invalidApprovalResponse = await client.SendAsync(invalidApprovalRequest);
        Assert.Equal(HttpStatusCode.BadRequest, invalidApprovalResponse.StatusCode);

        using var configureProfileRequest = Authorized(HttpMethod.Put, "/api/settings/business", currentSession.AccessToken!);
        configureProfileRequest.Content = JsonContent.Create(new
        {
            businessName = "Hotel sintético de integración",
            rtn = "08011999123456",
            address = "Tegucigalpa, Honduras",
            isvRate = 0.15m,
            touristTaxRate = 0.04m
        });
        var configureProfileResponse = await client.SendAsync(configureProfileRequest);
        Assert.Equal(HttpStatusCode.OK, configureProfileResponse.StatusCode);
        var configuredProfile = (await configureProfileResponse.Content.ReadFromJsonAsync<BusinessSettingsDto>())!;
        Assert.Equal(FiscalProfileStatus.Borrador.ToString(), configuredProfile.FiscalProfileStatus);

        using var approvalRequest = Authorized(HttpMethod.Post, "/api/settings/business/approve", currentSession.AccessToken!);
        approvalRequest.Content = JsonContent.Create(new
        {
            validFrom = HondurasToday(),
            approvalNote = "Perfil sintético aprobado por prueba automatizada"
        });
        var approvalResponse = await client.SendAsync(approvalRequest);
        Assert.Equal(HttpStatusCode.OK, approvalResponse.StatusCode);
        var approvedProfile = (await approvalResponse.Content.ReadFromJsonAsync<BusinessSettingsDto>())!;
        Assert.Equal(FiscalProfileStatus.Aprobado.ToString(), approvedProfile.FiscalProfileStatus);

        var caiId = await CreateActiveCaiAsync(factory.Services);
        var invoiceIdempotencyKey = Guid.NewGuid().ToString("D");
        using var invoiceAfterApprovalRequest = InvoiceRequest(currentSession.AccessToken!, caiId, invoiceIdempotencyKey);
        var invoiceAfterApprovalResponse = await client.SendAsync(invoiceAfterApprovalRequest);
        Assert.Equal(HttpStatusCode.Created, invoiceAfterApprovalResponse.StatusCode);
        var issuedInvoice = (await invoiceAfterApprovalResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal("001-001-01-00000001", issuedInvoice.CorrelativeNumber);
        Assert.Single(issuedInvoice.Items);
        await AssertInvoiceAccountingAndAuditAsync(factory.Services, issuedInvoice.Id);

        using var repeatedInvoiceRequest = InvoiceRequest(currentSession.AccessToken!, caiId, invoiceIdempotencyKey);
        var repeatedInvoiceResponse = await client.SendAsync(repeatedInvoiceRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedInvoiceResponse.StatusCode);
        var repeatedInvoice = (await repeatedInvoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(issuedInvoice.Id, repeatedInvoice.Id);
        await AssertInvoiceAccountingAndAuditAsync(factory.Services, issuedInvoice.Id);

        using var conflictingInvoiceRequest = InvoiceRequest(currentSession.AccessToken!, caiId, invoiceIdempotencyKey, unitPrice: 101m);
        var conflictingInvoiceResponse = await client.SendAsync(conflictingInvoiceRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictingInvoiceResponse.StatusCode);

        var debitAuthorizationId = await CreateDocumentAuthorizationAsync(
            client,
            currentSession.AccessToken!,
            InvoiceDocumentType.NotaDebito,
            "002-001-02-00000001");
        var debitKey = Guid.NewGuid().ToString("D");
        using var debitRequest = DebitNoteRequest(
            currentSession.AccessToken!,
            issuedInvoice,
            debitAuthorizationId,
            debitKey);
        var debitResponse = await client.SendAsync(debitRequest);
        Assert.Equal(HttpStatusCode.Created, debitResponse.StatusCode);
        var debitNote = (await debitResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(InvoiceDocumentType.NotaDebito.ToString(), debitNote.DocumentType);
        Assert.Equal(11.50m, debitNote.TotalAmount);
        await AssertAdjustmentAccountingAndAuditAsync(factory.Services, debitNote.Id, false);

        using var repeatedDebitRequest = DebitNoteRequest(
            currentSession.AccessToken!,
            issuedInvoice,
            debitAuthorizationId,
            debitKey);
        var repeatedDebitResponse = await client.SendAsync(repeatedDebitRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedDebitResponse.StatusCode);
        Assert.Equal(
            debitNote.Id,
            (await repeatedDebitResponse.Content.ReadFromJsonAsync<InvoiceDto>())!.Id);

        var creditAuthorizationId = await CreateDocumentAuthorizationAsync(
            client,
            currentSession.AccessToken!,
            InvoiceDocumentType.NotaCredito,
            "003-001-03-00000001");
        var creditKey = Guid.NewGuid().ToString("D");
        using var creditRequest = CreditNoteRequest(
            currentSession.AccessToken!,
            issuedInvoice,
            creditAuthorizationId,
            creditKey);
        var creditResponse = await client.SendAsync(creditRequest);
        Assert.Equal(HttpStatusCode.Created, creditResponse.StatusCode);
        var creditNote = (await creditResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(InvoiceDocumentType.NotaCredito.ToString(), creditNote.DocumentType);
        Assert.Equal(119m, creditNote.TotalAmount);
        Assert.Equal(0m, creditNote.BalanceDue);
        Assert.Equal(issuedInvoice.Items[0].Id, creditNote.Items[0].OriginalInvoiceItemId);
        await AssertAdjustmentAccountingAndAuditAsync(factory.Services, creditNote.Id, true);

        using var creditedInvoiceRequest = Authorized(HttpMethod.Get, $"/api/invoices/{issuedInvoice.Id}", currentSession.AccessToken!);
        using var creditedInvoiceResponse = await client.SendAsync(creditedInvoiceRequest);
        Assert.Equal(HttpStatusCode.OK, creditedInvoiceResponse.StatusCode);
        var creditedInvoice = (await creditedInvoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(119m, creditedInvoice.CreditedAmount);
        Assert.Equal(0m, creditedInvoice.BalanceDue);

        using var creditedInvoicePaymentRequest = PaymentRequest(
            currentSession.AccessToken!,
            Guid.NewGuid().ToString("D"),
            "Transferencia",
            1m,
            issuedInvoice.Id,
            externalReference: "TRX-FACTURA-ACREDITADA");
        using var creditedInvoicePaymentResponse = await client.SendAsync(creditedInvoicePaymentRequest);
        Assert.Equal(HttpStatusCode.Conflict, creditedInvoicePaymentResponse.StatusCode);

        using var repeatedCreditRequest = CreditNoteRequest(
            currentSession.AccessToken!,
            issuedInvoice,
            creditAuthorizationId,
            creditKey);
        var repeatedCreditResponse = await client.SendAsync(repeatedCreditRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedCreditResponse.StatusCode);
        Assert.Equal(
            creditNote.Id,
            (await repeatedCreditResponse.Content.ReadFromJsonAsync<InvoiceDto>())!.Id);

        using var excessCreditRequest = CreditNoteRequest(
            currentSession.AccessToken!,
            issuedInvoice,
            creditAuthorizationId,
            Guid.NewGuid().ToString("D"));
        var excessCreditResponse = await client.SendAsync(excessCreditRequest);
        Assert.Equal(HttpStatusCode.Conflict, excessCreditResponse.StatusCode);

        await AssertReservationLifecycleAsync(
            client,
            factory.Services,
            currentSession.AccessToken!);

        await AssertCashRegisterLifecycleAsync(
            client,
            factory.Services,
            currentSession.AccessToken!);

        await AssertPaymentLifecycleAsync(
            client,
            factory.Services,
            currentSession.AccessToken!,
            caiId);

        await AssertRefundLifecycleAsync(
            client,
            factory.Services,
            currentSession.AccessToken!,
            caiId,
            creditAuthorizationId);

        await AssertCardSettlementLifecycleAsync(
            client,
            factory.Services,
            currentSession.AccessToken!,
            caiId,
            creditAuthorizationId);

        await AssertConcurrentCheckInAndAtomicCheckoutAsync(
            client,
            factory.Services,
            currentSession.AccessToken!,
            caiId);

        using var retirementRequest = Authorized(HttpMethod.Post, "/api/settings/business/retire", currentSession.AccessToken!);
        retirementRequest.Content = JsonContent.Create(new
        {
            reason = "Retiro sintético para comprobar bloqueo de emisión"
        });
        var retirementResponse = await client.SendAsync(retirementRequest);
        Assert.Equal(HttpStatusCode.OK, retirementResponse.StatusCode);
        var retiredProfile = (await retirementResponse.Content.ReadFromJsonAsync<BusinessSettingsDto>())!;
        Assert.Equal(FiscalProfileStatus.Retirado.ToString(), retiredProfile.FiscalProfileStatus);

        using var repeatedInvoiceAfterRetirementRequest = InvoiceRequest(currentSession.AccessToken!, caiId, invoiceIdempotencyKey);
        var repeatedInvoiceAfterRetirementResponse = await client.SendAsync(repeatedInvoiceAfterRetirementRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedInvoiceAfterRetirementResponse.StatusCode);
        var repeatedInvoiceAfterRetirement = (await repeatedInvoiceAfterRetirementResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(issuedInvoice.Id, repeatedInvoiceAfterRetirement.Id);

        using var invoiceAfterRetirementRequest = InvoiceRequest(currentSession.AccessToken!, idempotencyKey: Guid.NewGuid().ToString("D"));
        var invoiceAfterRetirementResponse = await client.SendAsync(invoiceAfterRetirementRequest);
        Assert.Equal(HttpStatusCode.Conflict, invoiceAfterRetirementResponse.StatusCode);

        using var usersRequest = Authorized(HttpMethod.Get, "/api/users", currentSession.AccessToken!);
        var usersResponse = await client.SendAsync(usersRequest);
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);

        await AssertGuestDeletionPreservesFolioAsync(factory.Services);

        var rotateResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = currentSession.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        var rotatedSession = (await rotateResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var replayResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = currentSession.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        using var accessAfterReplayRequest = Authorized(HttpMethod.Get, "/api/rooms", rotatedSession.AccessToken!);
        var accessAfterReplay = await client.SendAsync(accessAfterReplayRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, accessAfterReplay.StatusCode);

        currentSession = await LoginAsync(client, "Integration!567");

        using var logoutRequest = Authorized(HttpMethod.Post, "/api/auth/logout", currentSession.AccessToken!);
        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        using var accessAfterLogoutRequest = Authorized(HttpMethod.Get, "/api/rooms", currentSession.AccessToken!);
        var accessAfterLogout = await client.SendAsync(accessAfterLogoutRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, accessAfterLogout.StatusCode);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = currentSession.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    private static async Task AssertGuestDeletionPreservesFolioAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roomType = new RoomType
        {
            Id = Guid.NewGuid(),
            Name = "Integración",
            PricePerNight = 100m,
            Capacity = 2
        };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = "IT-01",
            Floor = 1,
            RoomType = roomType
        };
        var guest = new Guest
        {
            Id = Guid.NewGuid(),
            FirstName = "Huésped",
            LastName = "Histórico",
            DocumentNumber = "IT-DOCUMENT-01"
        };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            Guest = guest,
            Room = room,
            CheckInDate = new DateOnly(2026, 9, 7),
            CheckOutDate = new DateOnly(2026, 9, 8),
            Status = ReservationStatus.CheckOut
        };
        var folio = new Folio
        {
            Id = Guid.NewGuid(),
            Reservation = reservation,
            Guest = guest,
            Room = room,
            Status = FolioStatus.Cerrado,
            ClosingDate = DateTime.UtcNow,
            TotalAmount = 115m,
            FolioItems =
            [
                new FolioItem
                {
                    Id = Guid.NewGuid(),
                    Description = "Hospedaje histórico",
                    Quantity = 1,
                    UnitPrice = 100m,
                    LineTotal = 100m,
                    ISVRate = 0.15m
                }
            ]
        };

        context.Add(folio);
        await context.SaveChangesAsync();
        guest.IsDeleted = true;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedFolio = await context.Folios
            .IgnoreQueryFilters()
            .SingleAsync(candidate => candidate.Id == folio.Id);
        Assert.False(persistedFolio.IsDeleted);

        var repository = scope.ServiceProvider.GetRequiredService<IFolioRepository>();
        var historicalFolio = await repository.GetByIdAsync(folio.Id);
        Assert.NotNull(historicalFolio);
        Assert.Equal("Huésped", historicalFolio.Guest.FirstName);
        Assert.Single(historicalFolio.FolioItems);
    }

    private static async Task SeedRoleMatrixUsersAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rolesBySystemKey = await context.Roles
            .Where(role => role.SystemKey != null)
            .ToDictionaryAsync(role => role.SystemKey!);

        var expectedRoleKeys = new[]
        {
            SystemRoleKeys.Administrator,
            SystemRoleKeys.Reception,
            SystemRoleKeys.Cashier,
            SystemRoleKeys.Accountant
        };
        Assert.All(expectedRoleKeys, roleKey => Assert.True(rolesBySystemKey.ContainsKey(roleKey)));

        var users = new[]
        {
            (Username: "matrix_admin", RoleKey: SystemRoleKeys.Administrator),
            (Username: "matrix_reception", RoleKey: SystemRoleKeys.Reception),
            (Username: "matrix_cashier", RoleKey: SystemRoleKeys.Cashier),
            (Username: "matrix_accountant", RoleKey: SystemRoleKeys.Accountant)
        };

        foreach (var (username, roleKey) in users)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@example.invalid",
                FirstName = "Matriz",
                LastName = rolesBySystemKey[roleKey].Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Matrix!23456"),
                IsActive = true,
                MustChangePassword = false
            };
            context.Users.Add(user);
            context.UserRoles.Add(new UserRole
            {
                User = user,
                Role = rolesBySystemKey[roleKey]
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task AssertEndpointStatusAsync(
        HttpClient client,
        string? accessToken,
        string path,
        HttpStatusCode expectedStatus)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private static async Task<int> AssertAnonymousControllerMatrixAsync(
        IServiceProvider services,
        HttpClient client)
    {
        var endpoints = services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .OrderBy(endpoint => endpoint.DisplayName, StringComparer.Ordinal)
            .ToList();

        foreach (var endpoint in endpoints)
        {
            var route = endpoint.RoutePattern.RawText
                ?? throw new InvalidOperationException($"Endpoint sin patrón: {endpoint.DisplayName}");
            var path = "/" + Regex.Replace(
                route.TrimStart('/'),
                @"\{(?<name>[^}:?]+)[^}]*\}",
                match => match.Groups["name"].Value switch
                {
                    "status" => "Disponible",
                    "documentType" => "Factura",
                    _ => Guid.Empty.ToString()
                });
            var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
                ?? throw new InvalidOperationException($"Endpoint sin verbo HTTP: {endpoint.DisplayName}");
            var isAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            foreach (var method in methods)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                if (method is "POST" or "PUT" or "PATCH")
                {
                    request.Content = path.EndsWith("/with-file", StringComparison.Ordinal)
                        ? new MultipartFormDataContent()
                        : JsonContent.Create(new { });
                }

                using var response = await client.SendAsync(request);
                if (isAnonymous)
                {
                    Assert.True(
                        response.StatusCode is HttpStatusCode.OK
                            or HttpStatusCode.BadRequest
                            or HttpStatusCode.Unauthorized
                            or HttpStatusCode.TooManyRequests,
                        $"{endpoint.DisplayName} ({method} {path}) no alcanzó su contrato público: {(int)response.StatusCode}.");
                }
                else
                {
                    Assert.True(
                        response.StatusCode == HttpStatusCode.Unauthorized,
                        $"{endpoint.DisplayName} ({method} {path}) devolvió {(int)response.StatusCode}; debe exigir autenticación.");
                }
            }
        }

        return endpoints.Count;
    }

    private static async Task AssertJsonEndpointStatusAsync(
        HttpClient client,
        string? accessToken,
        string path,
        HttpStatusCode expectedStatus)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { })
        };
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        using var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private static async Task AssertAuthorizationAttachmentSecurityAsync(
        HttpClient client,
        IServiceProvider services,
        string accessToken,
        string attachmentBaseDirectory,
        string attachmentDirectory)
    {
        var pdfBytes = System.Text.Encoding.ASCII.GetBytes(
            "%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF\n");
        Directory.CreateDirectory(attachmentBaseDirectory);
        var outsideFile = Path.Combine(attachmentBaseDirectory, "outside.pdf");
        await File.WriteAllBytesAsync(outsideFile, pdfBytes);

        try
        {
            using (var emptyRequest = AuthorizationAttachmentRequest(
                accessToken,
                $"QA-ATT-EMPTY-{Guid.NewGuid():N}",
                "evidencia.pdf",
                [],
                "application/pdf"))
            using (var emptyResponse = await client.SendAsync(emptyRequest))
            {
                Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode);
            }

            using (var falseContentRequest = AuthorizationAttachmentRequest(
                accessToken,
                $"QA-ATT-FALSE-{Guid.NewGuid():N}",
                "evidencia.pdf",
                System.Text.Encoding.UTF8.GetBytes("contenido que no es un documento PDF"),
                "application/pdf"))
            using (var falseContentResponse = await client.SendAsync(falseContentRequest))
            {
                Assert.Equal(HttpStatusCode.BadRequest, falseContentResponse.StatusCode);
                await AssertSafeProblemDetailsAsync(
                    falseContentResponse,
                    HttpStatusCode.BadRequest,
                    "Adjunto rechazado");
            }

            using (var falseTypeRequest = AuthorizationAttachmentRequest(
                accessToken,
                $"QA-ATT-TYPE-{Guid.NewGuid():N}",
                "evidencia.pdf",
                pdfBytes,
                "text/plain"))
            using (var falseTypeResponse = await client.SendAsync(falseTypeRequest))
            {
                Assert.Equal(HttpStatusCode.BadRequest, falseTypeResponse.StatusCode);
            }

            using (var oversizedRequest = AuthorizationAttachmentRequest(
                accessToken,
                $"QA-ATT-SIZE-{Guid.NewGuid():N}",
                "evidencia.pdf",
                new byte[10 * 1024 * 1024 + 1],
                "application/pdf"))
            using (var oversizedResponse = await client.SendAsync(oversizedRequest))
            {
                Assert.Equal(HttpStatusCode.BadRequest, oversizedResponse.StatusCode);
            }

            var submittedFileName = $"../../{Guid.NewGuid():N}.pdf";
            using var validRequest = AuthorizationAttachmentRequest(
                accessToken,
                $"QA-ATT-VALID-{Guid.NewGuid():N}",
                submittedFileName,
                pdfBytes,
                "application/pdf");
            using var validResponse = await client.SendAsync(validRequest);
            Assert.Equal(HttpStatusCode.Created, validResponse.StatusCode);
            var serializedAuthorization = await validResponse.Content.ReadAsStringAsync();
            var authorization = System.Text.Json.JsonSerializer.Deserialize<DocumentAuthorizationDto>(
                serializedAuthorization,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            Assert.True(authorization.HasAttachment);
            Assert.DoesNotContain("attachmentPath", serializedAuthorization, StringComparison.OrdinalIgnoreCase);

            using (var scope = services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var persisted = await context.DocumentAuthorizations.SingleAsync(
                    candidate => candidate.Id == authorization.Id);
                Assert.Equal($"{authorization.Id:N}.pdf", persisted.AttachmentPath);
                Assert.False(Path.IsPathRooted(persisted.AttachmentPath));

                var traversalAuthorization = new DocumentAuthorization
                {
                    Id = Guid.NewGuid(),
                    DocumentType = InvoiceDocumentType.NotaDebito,
                    CAINumber = $"QA-ATT-PATH-{Guid.NewGuid():N}",
                    IssueDate = HondurasToday(),
                    DueDate = HondurasToday().AddDays(30),
                    InitialRange = "099-099-02-00000001",
                    FinalRange = "099-099-02-00000100",
                    CurrentCorrelative = "099-099-02-00000001",
                    Status = CAIStatus.Activo,
                    AttachmentPath = Path.Combine("..", "outside.pdf")
                };
                context.DocumentAuthorizations.Add(traversalAuthorization);
                await context.SaveChangesAsync();

                using var traversalRequest = Authorized(
                    HttpMethod.Get,
                    $"/api/document-authorizations/{traversalAuthorization.Id}/file",
                    accessToken);
                using var traversalResponse = await client.SendAsync(traversalRequest);
                Assert.Equal(HttpStatusCode.NotFound, traversalResponse.StatusCode);
            }

            Assert.True(File.Exists(Path.Combine(attachmentDirectory, $"{authorization.Id:N}.pdf")));
            Assert.False(File.Exists(Path.Combine(attachmentBaseDirectory, Path.GetFileName(submittedFileName))));

            var receptionUsername = $"reception_{Guid.NewGuid():N}"[..28];
            using var registerReceptionRequest = Authorized(HttpMethod.Post, "/api/users", accessToken);
            registerReceptionRequest.Content = JsonContent.Create(new
            {
                username = receptionUsername,
                password = "Reception!234",
                email = $"{receptionUsername}@example.invalid",
                firstName = "Recepción",
                lastName = "Adjuntos",
                roles = new[] { "Recepcion" }
            });
            using var registerReceptionResponse = await client.SendAsync(registerReceptionRequest);
            Assert.Equal(HttpStatusCode.Created, registerReceptionResponse.StatusCode);

            var initialReceptionSession = await LoginAsync(client, "Reception!234", receptionUsername);
            using var receptionPasswordChange = Authorized(
                HttpMethod.Post,
                "/api/auth/change-password",
                initialReceptionSession.AccessToken!);
            receptionPasswordChange.Content = JsonContent.Create(new
            {
                currentPassword = "Reception!234",
                newPassword = "Reception!345"
            });
            using var receptionPasswordChangeResponse = await client.SendAsync(receptionPasswordChange);
            Assert.Equal(HttpStatusCode.OK, receptionPasswordChangeResponse.StatusCode);

            var receptionSession = await LoginAsync(client, "Reception!345", receptionUsername);
            Assert.DoesNotContain(
                PermissionNames.ManageTaxes,
                receptionSession.User!.Permissions,
                StringComparer.Ordinal);

            using var forbiddenDownloadRequest = Authorized(
                HttpMethod.Get,
                $"/api/document-authorizations/{authorization.Id}/file",
                receptionSession.AccessToken!);
            using var forbiddenDownloadResponse = await client.SendAsync(forbiddenDownloadRequest);
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenDownloadResponse.StatusCode);

            using var downloadRequest = Authorized(
                HttpMethod.Get,
                $"/api/document-authorizations/{authorization.Id}/file",
                accessToken);
            using var downloadResponse = await client.SendAsync(downloadRequest);
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
            Assert.Equal("no-store", downloadResponse.Headers.CacheControl?.ToString());
            Assert.Equal("nosniff", downloadResponse.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal(pdfBytes, await downloadResponse.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            if (Directory.Exists(attachmentBaseDirectory))
            {
                Directory.Delete(attachmentBaseDirectory, recursive: true);
            }
        }
    }

    private static HttpRequestMessage AuthorizationAttachmentRequest(
        string accessToken,
        string caiNumber,
        string fileName,
        byte[] fileBytes,
        string contentType)
    {
        var request = Authorized(HttpMethod.Post, "/api/document-authorizations/with-file", accessToken);
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("NotaDebito"), "documentType");
        content.Add(new StringContent(caiNumber), "caiNumber");
        content.Add(new StringContent(HondurasToday().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)), "issueDate");
        content.Add(new StringContent(HondurasToday().AddDays(30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)), "dueDate");
        content.Add(new StringContent("099-099-02-00000001"), "initialRange");
        content.Add(new StringContent("099-099-02-00000100"), "finalRange");
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);
        request.Content = content;
        return request;
    }

    private static async Task AssertCashRegisterLifecycleAsync(
        HttpClient client,
        IServiceProvider services,
        string accessToken)
    {
        using var createRequest = Authorized(HttpMethod.Post, "/api/cash-registers", accessToken);
        createRequest.Content = JsonContent.Create(new
        {
            name = $"Caja integración {Guid.NewGuid():N}"[..28],
            description = "Caja sintética para probar apertura y arqueo"
        });
        using var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var cashRegister = (await createResponse.Content.ReadFromJsonAsync<CashRegisterDto>())!;
        Assert.False(cashRegister.IsOpen);
        Assert.Equal(0m, cashRegister.CurrentBalance);

        using var missingKeyRequest = Authorized(
            HttpMethod.Post,
            $"/api/cash-registers/{cashRegister.Id}/open",
            accessToken);
        missingKeyRequest.Content = JsonContent.Create(new { initialAmount = 1000m });
        using var missingKeyResponse = await client.SendAsync(missingKeyRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingKeyResponse.StatusCode);

        var firstOpenKey = Guid.NewGuid().ToString("D");
        using var openRequest = CashOperationRequest(
            accessToken,
            cashRegister.Id,
            "open",
            firstOpenKey,
            new { initialAmount = 1000m });
        using var openResponse = await client.SendAsync(openRequest);
        Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);
        var opening = (await openResponse.Content.ReadFromJsonAsync<CashRegisterOperationDto>())!;
        Assert.Equal(1000m, opening.CurrentBalance);

        using var repeatedOpenRequest = CashOperationRequest(
            accessToken,
            cashRegister.Id,
            "open",
            firstOpenKey,
            new { initialAmount = 1000m });
        using var repeatedOpenResponse = await client.SendAsync(repeatedOpenRequest);
        Assert.Equal(HttpStatusCode.OK, repeatedOpenResponse.StatusCode);
        Assert.Equal(
            opening.MovementId,
            (await repeatedOpenResponse.Content.ReadFromJsonAsync<CashRegisterOperationDto>())!.MovementId);

        using var conflictingOpenRequest = CashOperationRequest(
            accessToken,
            cashRegister.Id,
            "open",
            firstOpenKey,
            new { initialAmount = 1001m });
        using var conflictingOpenResponse = await client.SendAsync(conflictingOpenRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictingOpenResponse.StatusCode);

        var firstCloseKey = Guid.NewGuid().ToString("D");
        using var unexplainedCloseRequest = CashOperationRequest(
            accessToken,
            cashRegister.Id,
            "close",
            firstCloseKey,
            new { countedAmount = 999m, expectedAmount = 1m, notes = (string?)null });
        using var unexplainedCloseResponse = await client.SendAsync(unexplainedCloseRequest);
        Assert.Equal(HttpStatusCode.BadRequest, unexplainedCloseResponse.StatusCode);

        using var closeRequest = CashOperationRequest(
            accessToken,
            cashRegister.Id,
            "close",
            firstCloseKey,
            new { countedAmount = 999m, expectedAmount = 1m, notes = "Faltante comprobado en arqueo" });
        using var closeResponse = await client.SendAsync(closeRequest);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closing = (await closeResponse.Content.ReadFromJsonAsync<CashRegisterOperationDto>())!;
        Assert.Equal(1000m, closing.ExpectedAmount);
        Assert.Equal(999m, closing.CountedAmount);
        Assert.Equal(-1m, closing.Difference);

        using var repeatedCloseRequest = CashOperationRequest(
            accessToken,
            cashRegister.Id,
            "close",
            firstCloseKey,
            new { countedAmount = 999m, expectedAmount = 1m, notes = "Faltante comprobado en arqueo" });
        using var repeatedCloseResponse = await client.SendAsync(repeatedCloseRequest);
        Assert.Equal(HttpStatusCode.OK, repeatedCloseResponse.StatusCode);
        Assert.Equal(
            closing.MovementId,
            (await repeatedCloseResponse.Content.ReadFromJsonAsync<CashRegisterOperationDto>())!.MovementId);

        using var conflictingCloseRequest = CashOperationRequest(
            accessToken,
            cashRegister.Id,
            "close",
            firstCloseKey,
            new { countedAmount = 1000m, notes = (string?)null });
        using var conflictingCloseResponse = await client.SendAsync(conflictingCloseRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictingCloseResponse.StatusCode);

        using var registersRequest = Authorized(HttpMethod.Get, "/api/cash-registers", accessToken);
        using var registersResponse = await client.SendAsync(registersRequest);
        Assert.Equal(HttpStatusCode.OK, registersResponse.StatusCode);
        var registers = (await registersResponse.Content.ReadFromJsonAsync<List<CashRegisterDto>>())!;
        var closedRegister = registers.Single(candidate => candidate.Id == cashRegister.Id);
        Assert.False(closedRegister.IsOpen);
        Assert.Equal(0m, closedRegister.CurrentBalance);

        var concurrentOpenRequests = new[]
        {
            CashOperationRequest(accessToken, cashRegister.Id, "open", Guid.NewGuid().ToString("D"), new { initialAmount = 500m }),
            CashOperationRequest(accessToken, cashRegister.Id, "open", Guid.NewGuid().ToString("D"), new { initialAmount = 500m })
        };
        var concurrentOpenResponses = await Task.WhenAll(concurrentOpenRequests.Select(client.SendAsync));
        Assert.Single(concurrentOpenResponses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(concurrentOpenResponses, response => response.StatusCode == HttpStatusCode.Conflict);

        var concurrentCloseRequests = new[]
        {
            CashOperationRequest(accessToken, cashRegister.Id, "close", Guid.NewGuid().ToString("D"), new { countedAmount = 500m, notes = (string?)null }),
            CashOperationRequest(accessToken, cashRegister.Id, "close", Guid.NewGuid().ToString("D"), new { countedAmount = 500m, notes = (string?)null })
        };
        var concurrentCloseResponses = await Task.WhenAll(concurrentCloseRequests.Select(client.SendAsync));
        Assert.Single(concurrentCloseResponses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(concurrentCloseResponses, response => response.StatusCode == HttpStatusCode.Conflict);

        using var movementsRequest = Authorized(
            HttpMethod.Get,
            $"/api/cash-registers/{cashRegister.Id}/movements",
            accessToken);
        using var movementsResponse = await client.SendAsync(movementsRequest);
        Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);
        var movements = (await movementsResponse.Content.ReadFromJsonAsync<List<CashMovementDto>>())!;
        Assert.Equal(4, movements.Count);
        var firstClosingMovement = movements.Single(movement => movement.Id == closing.MovementId);
        Assert.Equal(1000m, firstClosingMovement.ExpectedAmount);
        Assert.Equal(999m, firstClosingMovement.CountedAmount);
        Assert.Equal(-1m, firstClosingMovement.Difference);
        Assert.Equal("Faltante comprobado en arqueo", firstClosingMovement.Notes);

        using var assertScope = services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await assertContext.CashMovements.CountAsync(movement =>
            movement.CashRegisterId == cashRegister.Id && movement.MovementType == CashMovementType.Apertura));
        Assert.Equal(2, await assertContext.CashMovements.CountAsync(movement =>
            movement.CashRegisterId == cashRegister.Id && movement.MovementType == CashMovementType.Cierre));
        Assert.Equal(2, await assertContext.AuditLogs.CountAsync(log =>
            log.EntityId == cashRegister.Id && log.Action == "OpenCashRegister"));
        Assert.Equal(2, await assertContext.AuditLogs.CountAsync(log =>
            log.EntityId == cashRegister.Id && log.Action == "CloseCashRegister"));
        var cashMovementIds = await assertContext.CashMovements
            .Where(movement => movement.CashRegisterId == cashRegister.Id)
            .Select(movement => movement.Id)
            .ToListAsync();
        Assert.Equal(4, await assertContext.IdempotencyRecords.CountAsync(record =>
            cashMovementIds.Contains(record.ResourceId)));

        foreach (var request in concurrentOpenRequests.Concat(concurrentCloseRequests))
            request.Dispose();
        foreach (var response in concurrentOpenResponses.Concat(concurrentCloseResponses))
            response.Dispose();
    }

    private static async Task AssertPaymentLifecycleAsync(
        HttpClient client,
        IServiceProvider services,
        string accessToken,
        Guid caiId)
    {
        using var invoiceRequest = InvoiceRequest(
            accessToken,
            caiId,
            Guid.NewGuid().ToString("D"));
        using var invoiceResponse = await client.SendAsync(invoiceRequest);
        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(InvoiceStatus.Emitida.ToString(), invoice.Status);
        Assert.Equal(0m, invoice.PaidAmount);
        Assert.Equal(119m, invoice.BalanceDue);
        Assert.Null(invoice.PaymentMethod);

        string fiscalHashBeforePayment;
        string fiscalSnapshotBeforePayment;
        using (var initialAssertScope = services.CreateScope())
        {
            var initialContext = initialAssertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var persistedInitialInvoice = await initialContext.Invoices.SingleAsync(candidate => candidate.Id == invoice.Id);
            fiscalHashBeforePayment = persistedInitialInvoice.FiscalHash!;
            fiscalSnapshotBeforePayment = persistedInitialInvoice.FiscalSnapshotJson!;
        }

        using var createRegisterRequest = Authorized(HttpMethod.Post, "/api/cash-registers", accessToken);
        createRegisterRequest.Content = JsonContent.Create(new
        {
            name = $"Caja pagos {Guid.NewGuid():N}"[..25],
            description = "Caja sintética para auxiliar de pagos"
        });
        using var createRegisterResponse = await client.SendAsync(createRegisterRequest);
        Assert.Equal(HttpStatusCode.Created, createRegisterResponse.StatusCode);
        var register = (await createRegisterResponse.Content.ReadFromJsonAsync<CashRegisterDto>())!;

        using var closedCashRequest = PaymentRequest(
            accessToken,
            Guid.NewGuid().ToString("D"),
            "Efectivo",
            1m,
            invoice.Id,
            register.Id,
            cashReceived: 1m);
        using var closedCashResponse = await client.SendAsync(closedCashRequest);
        Assert.Equal(HttpStatusCode.Conflict, closedCashResponse.StatusCode);

        using var openRequest = CashOperationRequest(
            accessToken,
            register.Id,
            "open",
            Guid.NewGuid().ToString("D"),
            new { initialAmount = 25m });
        using var openResponse = await client.SendAsync(openRequest);
        Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);

        using var missingKeyRequest = PaymentRequest(
            accessToken,
            null,
            "Efectivo",
            100m,
            invoice.Id,
            register.Id,
            cashReceived: 200m);
        using var missingKeyResponse = await client.SendAsync(missingKeyRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingKeyResponse.StatusCode);

        var cashKey = Guid.NewGuid().ToString("D");
        using var cashPaymentRequest = PaymentRequest(
            accessToken,
            cashKey,
            "Efectivo",
            100m,
            invoice.Id,
            register.Id,
            cashReceived: 200m);
        using var cashPaymentResponse = await client.SendAsync(cashPaymentRequest);
        Assert.Equal(HttpStatusCode.Created, cashPaymentResponse.StatusCode);
        var cashPayment = (await cashPaymentResponse.Content.ReadFromJsonAsync<PaymentDto>())!;
        Assert.Equal(PaymentMethod.Efectivo.ToString(), cashPayment.Method);
        Assert.Equal(100m, cashPayment.Amount);
        Assert.Equal(200m, cashPayment.CashReceived);
        Assert.Equal(100m, cashPayment.CashChange);
        Assert.Single(cashPayment.Applications);

        using var repeatedCashRequest = PaymentRequest(
            accessToken,
            cashKey,
            "Efectivo",
            100m,
            invoice.Id,
            register.Id,
            cashReceived: 200m);
        using var repeatedCashResponse = await client.SendAsync(repeatedCashRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedCashResponse.StatusCode);
        Assert.Equal(
            cashPayment.Id,
            (await repeatedCashResponse.Content.ReadFromJsonAsync<PaymentDto>())!.Id);

        using var conflictingCashRequest = PaymentRequest(
            accessToken,
            cashKey,
            "Efectivo",
            99m,
            invoice.Id,
            register.Id,
            cashReceived: 200m);
        using var conflictingCashResponse = await client.SendAsync(conflictingCashRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictingCashResponse.StatusCode);

        using var partialInvoiceRequest = Authorized(HttpMethod.Get, $"/api/invoices/{invoice.Id}", accessToken);
        using var partialInvoiceResponse = await client.SendAsync(partialInvoiceRequest);
        Assert.Equal(HttpStatusCode.OK, partialInvoiceResponse.StatusCode);
        var partialInvoice = (await partialInvoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(100m, partialInvoice.PaidAmount);
        Assert.Equal(19m, partialInvoice.BalanceDue);
        Assert.Equal(InvoiceStatus.Emitida.ToString(), partialInvoice.Status);
        Assert.Equal(PaymentMethod.Efectivo.ToString(), partialInvoice.PaymentMethod);

        var concurrentRequests = new[]
        {
            PaymentRequest(
                accessToken,
                Guid.NewGuid().ToString("D"),
                "Transferencia",
                19m,
                invoice.Id,
                externalReference: "TRX-INTEGRACION-A"),
            PaymentRequest(
                accessToken,
                Guid.NewGuid().ToString("D"),
                "Transferencia",
                19m,
                invoice.Id,
                externalReference: "TRX-INTEGRACION-B")
        };
        var concurrentResponses = await Task.WhenAll(concurrentRequests.Select(client.SendAsync));
        Assert.Single(concurrentResponses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(concurrentResponses, response => response.StatusCode == HttpStatusCode.Conflict);
        var transferPayment = (await concurrentResponses
            .Single(response => response.StatusCode == HttpStatusCode.Created)
            .Content
            .ReadFromJsonAsync<PaymentDto>())!;

        using var paidInvoiceRequest = Authorized(HttpMethod.Get, $"/api/invoices/{invoice.Id}", accessToken);
        using var paidInvoiceResponse = await client.SendAsync(paidInvoiceRequest);
        Assert.Equal(HttpStatusCode.OK, paidInvoiceResponse.StatusCode);
        var paidInvoice = (await paidInvoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(119m, paidInvoice.PaidAmount);
        Assert.Equal(0m, paidInvoice.BalanceDue);
        Assert.Equal(InvoiceStatus.Pagada.ToString(), paidInvoice.Status);
        Assert.Equal("Mixto", paidInvoice.PaymentMethod);

        using var paymentListRequest = Authorized(HttpMethod.Get, $"/api/payments?invoiceId={invoice.Id}", accessToken);
        using var paymentListResponse = await client.SendAsync(paymentListRequest);
        Assert.Equal(HttpStatusCode.OK, paymentListResponse.StatusCode);
        var paymentList = (await paymentListResponse.Content.ReadFromJsonAsync<List<PaymentDto>>())!;
        Assert.Equal(2, paymentList.Count);
        Assert.Equal(119m, paymentList.Sum(payment => payment.Amount));

        using var assertScope = services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedInvoice = await context.Invoices
            .Include(candidate => candidate.PaymentApplications)
            .ThenInclude(application => application.Payment)
            .SingleAsync(candidate => candidate.Id == invoice.Id);
        Assert.Equal(2, persistedInvoice.PaymentApplications.Count);
        Assert.Equal(119m, persistedInvoice.PaymentApplications.Sum(application => application.Amount));
        Assert.Equal(2, persistedInvoice.PaymentApplications.Select(application => application.PaymentId).Distinct().Count());
        Assert.Null(persistedInvoice.PaymentMethod);
        Assert.Null(persistedInvoice.CashReceived);
        Assert.Null(persistedInvoice.CashChange);
        Assert.Equal(fiscalHashBeforePayment, persistedInvoice.FiscalHash);
        Assert.Equal(fiscalSnapshotBeforePayment, persistedInvoice.FiscalSnapshotJson);

        var paymentIds = persistedInvoice.PaymentApplications
            .Select(application => application.PaymentId)
            .ToList();
        var entries = await context.AccountingEntries
            .Include(entry => entry.EntryItems)
            .ThenInclude(item => item.Account)
            .Where(entry => paymentIds.Contains(entry.ReferenceId!.Value))
            .ToListAsync();
        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry => Assert.Equal(
            entry.EntryItems.Sum(item => item.Debit),
            entry.EntryItems.Sum(item => item.Credit)));
        Assert.Contains(entries, entry => entry.EntryItems.Any(item => item.Account.AccountNumber == "1101" && item.Debit == 100m));
        Assert.Contains(entries, entry => entry.EntryItems.Any(item => item.Account.AccountNumber == "1102" && item.Debit == 19m));
        Assert.All(entries, entry => Assert.Contains(entry.EntryItems, item => item.Account.AccountNumber == "1103" && item.Credit == entry.EntryItems.Sum(line => line.Debit)));

        var cashMovement = await context.CashMovements.SingleAsync(movement => movement.ReferenceId == cashPayment.Id);
        Assert.Equal(100m, cashMovement.Amount);
        Assert.Equal(125m, cashMovement.BalanceAfter);
        Assert.DoesNotContain(await context.CashMovements.ToListAsync(), movement => movement.ReferenceId == transferPayment.Id);
        Assert.Equal(2, await context.AuditLogs.CountAsync(log => log.Action == "CreatePayment" && paymentIds.Contains(log.EntityId!.Value)));
        Assert.Equal(2, await context.IdempotencyRecords.CountAsync(record => record.Scope == "payment:create" && paymentIds.Contains(record.ResourceId)));

        foreach (var request in concurrentRequests)
            request.Dispose();
        foreach (var response in concurrentResponses)
            response.Dispose();
    }

    private static async Task AssertRefundLifecycleAsync(
        HttpClient client,
        IServiceProvider services,
        string accessToken,
        Guid caiId,
        Guid creditAuthorizationId)
    {
        using var invoiceRequest = InvoiceRequest(
            accessToken,
            caiId,
            Guid.NewGuid().ToString("D"));
        using var invoiceResponse = await client.SendAsync(invoiceRequest);
        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(119m, invoice.TotalAmount);

        using var createRegisterRequest = Authorized(HttpMethod.Post, "/api/cash-registers", accessToken);
        createRegisterRequest.Content = JsonContent.Create(new
        {
            name = $"Caja reembolsos {Guid.NewGuid():N}"[..29],
            description = "Caja sintética para reembolsos trazables"
        });
        using var createRegisterResponse = await client.SendAsync(createRegisterRequest);
        Assert.Equal(HttpStatusCode.Created, createRegisterResponse.StatusCode);
        var register = (await createRegisterResponse.Content.ReadFromJsonAsync<CashRegisterDto>())!;

        using var openRequest = CashOperationRequest(
            accessToken,
            register.Id,
            "open",
            Guid.NewGuid().ToString("D"),
            new { initialAmount = 30m });
        using var openResponse = await client.SendAsync(openRequest);
        Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);

        using var paymentRequest = PaymentRequest(
            accessToken,
            Guid.NewGuid().ToString("D"),
            PaymentMethod.Efectivo.ToString(),
            invoice.TotalAmount,
            invoice.Id,
            register.Id,
            cashReceived: 150m);
        using var paymentResponse = await client.SendAsync(paymentRequest);
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentDto>())!;
        Assert.Equal(119m, payment.Amount);
        Assert.Equal(31m, payment.CashChange);

        using var creditRequest = CreditNoteRequest(
            accessToken,
            invoice,
            creditAuthorizationId,
            Guid.NewGuid().ToString("D"));
        using var creditResponse = await client.SendAsync(creditRequest);
        Assert.Equal(HttpStatusCode.Created, creditResponse.StatusCode);
        var creditNote = (await creditResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(119m, creditNote.TotalAmount);
        await AssertAdjustmentAccountingAndAuditAsync(
            services,
            creditNote.Id,
            isCredit: true,
            expectedCustomerCredit: true);

        using var missingKeyRequest = RefundRequest(
            accessToken,
            payment.Id,
            null,
            119m,
            creditNote.Id,
            register.Id,
            "Devolución total al huésped");
        using var missingKeyResponse = await client.SendAsync(missingKeyRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingKeyResponse.StatusCode);

        using var emptyReasonRequest = RefundRequest(
            accessToken,
            payment.Id,
            Guid.NewGuid().ToString("D"),
            119m,
            creditNote.Id,
            register.Id,
            "   ");
        using var emptyReasonResponse = await client.SendAsync(emptyReasonRequest);
        Assert.Equal(HttpStatusCode.BadRequest, emptyReasonResponse.StatusCode);

        var refundKeys = new[] { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") };
        var concurrentRequests = refundKeys
            .Select(key => RefundRequest(
                accessToken,
                payment.Id,
                key,
                119m,
                creditNote.Id,
                register.Id,
                "Devolución total al huésped"))
            .ToArray();
        var concurrentResponses = await Task.WhenAll(concurrentRequests.Select(client.SendAsync));
        var concurrentDetails = await Task.WhenAll(concurrentResponses.Select(async response =>
            $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}"));
        Assert.True(
            concurrentResponses.Count(response => response.StatusCode == HttpStatusCode.Created) == 1,
            string.Join(Environment.NewLine, concurrentDetails));
        Assert.True(
            concurrentResponses.Count(response => response.StatusCode == HttpStatusCode.Conflict) == 1,
            string.Join(Environment.NewLine, concurrentDetails));
        var successfulIndex = Array.FindIndex(
            concurrentResponses,
            response => response.StatusCode == HttpStatusCode.Created);
        var refund = (await concurrentResponses[successfulIndex].Content.ReadFromJsonAsync<RefundDto>())!;
        Assert.Equal(payment.Id, refund.PaymentId);
        Assert.Equal(payment.PaymentNumber, refund.PaymentNumber);
        Assert.Equal(PaymentMethod.Efectivo.ToString(), refund.Method);
        Assert.Equal(119m, refund.Amount);
        Assert.Equal(register.Id, refund.CashRegisterId);
        Assert.Single(refund.Applications);
        Assert.Equal(creditNote.Id, refund.Applications[0].CreditNoteId);

        using var repeatedRequest = RefundRequest(
            accessToken,
            payment.Id,
            refundKeys[successfulIndex],
            119m,
            creditNote.Id,
            register.Id,
            "Devolución total al huésped");
        using var repeatedResponse = await client.SendAsync(repeatedRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedResponse.StatusCode);
        Assert.Equal(refund.Id, (await repeatedResponse.Content.ReadFromJsonAsync<RefundDto>())!.Id);

        using var conflictingRequest = RefundRequest(
            accessToken,
            payment.Id,
            refundKeys[successfulIndex],
            118m,
            creditNote.Id,
            register.Id,
            "Devolución total al huésped");
        using var conflictingResponse = await client.SendAsync(conflictingRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictingResponse.StatusCode);

        using var excessRequest = RefundRequest(
            accessToken,
            payment.Id,
            Guid.NewGuid().ToString("D"),
            1m,
            creditNote.Id,
            register.Id,
            "Segundo reembolso improcedente");
        using var excessResponse = await client.SendAsync(excessRequest);
        Assert.Equal(HttpStatusCode.Conflict, excessResponse.StatusCode);

        using var refundsRequest = Authorized(
            HttpMethod.Get,
            $"/api/payments/{payment.Id}/refunds",
            accessToken);
        using var refundsResponse = await client.SendAsync(refundsRequest);
        Assert.Equal(HttpStatusCode.OK, refundsResponse.StatusCode);
        var refunds = (await refundsResponse.Content.ReadFromJsonAsync<List<RefundDto>>())!;
        Assert.Single(refunds);
        Assert.Equal(refund.Id, refunds[0].Id);

        using var paymentDetailRequest = Authorized(HttpMethod.Get, $"/api/payments/{payment.Id}", accessToken);
        using var paymentDetailResponse = await client.SendAsync(paymentDetailRequest);
        Assert.Equal(HttpStatusCode.OK, paymentDetailResponse.StatusCode);
        var refundedPayment = (await paymentDetailResponse.Content.ReadFromJsonAsync<PaymentDto>())!;
        Assert.Equal(PaymentStatus.Reembolsado.ToString(), refundedPayment.Status);
        Assert.Equal(119m, refundedPayment.RefundedAmount);
        Assert.Single(refundedPayment.Refunds);

        using var invoiceDetailRequest = Authorized(HttpMethod.Get, $"/api/invoices/{invoice.Id}", accessToken);
        using var invoiceDetailResponse = await client.SendAsync(invoiceDetailRequest);
        Assert.Equal(HttpStatusCode.OK, invoiceDetailResponse.StatusCode);
        var creditedInvoice = (await invoiceDetailResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(119m, creditedInvoice.PaidAmount);
        Assert.Equal(119m, creditedInvoice.CreditedAmount);
        Assert.Equal(0m, creditedInvoice.BalanceDue);

        using var transferInvoiceRequest = InvoiceRequest(
            accessToken,
            caiId,
            Guid.NewGuid().ToString("D"));
        using var transferInvoiceResponse = await client.SendAsync(transferInvoiceRequest);
        Assert.Equal(HttpStatusCode.Created, transferInvoiceResponse.StatusCode);
        var transferInvoice = (await transferInvoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;

        using var transferPaymentRequest = PaymentRequest(
            accessToken,
            Guid.NewGuid().ToString("D"),
            PaymentMethod.Transferencia.ToString(),
            transferInvoice.TotalAmount,
            transferInvoice.Id,
            externalReference: "TRX-COBRO-DEVOLUCION");
        using var transferPaymentResponse = await client.SendAsync(transferPaymentRequest);
        Assert.Equal(HttpStatusCode.Created, transferPaymentResponse.StatusCode);
        var transferPayment = (await transferPaymentResponse.Content.ReadFromJsonAsync<PaymentDto>())!;

        using var transferCreditRequest = CreditNoteRequest(
            accessToken,
            transferInvoice,
            creditAuthorizationId,
            Guid.NewGuid().ToString("D"));
        using var transferCreditResponse = await client.SendAsync(transferCreditRequest);
        Assert.Equal(HttpStatusCode.Created, transferCreditResponse.StatusCode);
        var transferCreditNote = (await transferCreditResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        await AssertAdjustmentAccountingAndAuditAsync(
            services,
            transferCreditNote.Id,
            isCredit: true,
            expectedCustomerCredit: true);

        using var missingReferenceRequest = RefundRequest(
            accessToken,
            transferPayment.Id,
            Guid.NewGuid().ToString("D"),
            transferInvoice.TotalAmount,
            transferCreditNote.Id,
            null,
            "Devolución mediante transferencia");
        using var missingReferenceResponse = await client.SendAsync(missingReferenceRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingReferenceResponse.StatusCode);

        using var transferRefundRequest = RefundRequest(
            accessToken,
            transferPayment.Id,
            Guid.NewGuid().ToString("D"),
            transferInvoice.TotalAmount,
            transferCreditNote.Id,
            null,
            "Devolución mediante transferencia",
            externalReference: "TRX-DEVOLUCION-01");
        using var transferRefundResponse = await client.SendAsync(transferRefundRequest);
        Assert.Equal(HttpStatusCode.Created, transferRefundResponse.StatusCode);
        var transferRefund = (await transferRefundResponse.Content.ReadFromJsonAsync<RefundDto>())!;
        Assert.Equal(PaymentMethod.Transferencia.ToString(), transferRefund.Method);
        Assert.Equal("TRX-DEVOLUCION-01", transferRefund.ExternalReference);
        Assert.Null(transferRefund.CashRegisterId);

        using var assertScope = services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRefund = await context.Refunds
            .Include(candidate => candidate.Applications)
            .SingleAsync(candidate => candidate.Id == refund.Id);
        Assert.Equal(RefundStatus.Confirmado, persistedRefund.Status);
        Assert.Single(persistedRefund.Applications);
        Assert.Equal(119m, persistedRefund.Applications.Single().Amount);

        var entry = await context.AccountingEntries
            .Include(candidate => candidate.EntryItems)
            .ThenInclude(item => item.Account)
            .SingleAsync(candidate => candidate.ReferenceId == refund.Id);
        Assert.Equal(entry.EntryItems.Sum(item => item.Debit), entry.EntryItems.Sum(item => item.Credit));
        Assert.Equal(119m, entry.EntryItems.Single(item => item.Account.AccountNumber == "2105").Debit);
        Assert.Equal(119m, entry.EntryItems.Single(item => item.Account.AccountNumber == "1101").Credit);

        var cashMovement = await context.CashMovements.SingleAsync(movement => movement.ReferenceId == refund.Id);
        Assert.Equal(CashMovementType.Egreso, cashMovement.MovementType);
        Assert.Equal(119m, cashMovement.Amount);
        Assert.Equal(30m, cashMovement.BalanceAfter);
        Assert.Single(await context.AuditLogs
            .Where(log => log.Action == "CreateRefund" && log.EntityId == refund.Id)
            .ToListAsync());
        Assert.Single(await context.IdempotencyRecords
            .Where(record => record.Scope == "payment:refund" && record.ResourceId == refund.Id)
            .ToListAsync());

        var transferEntry = await context.AccountingEntries
            .Include(candidate => candidate.EntryItems)
            .ThenInclude(item => item.Account)
            .SingleAsync(candidate => candidate.ReferenceId == transferRefund.Id);
        Assert.Equal(
            transferEntry.EntryItems.Sum(item => item.Debit),
            transferEntry.EntryItems.Sum(item => item.Credit));
        Assert.Equal(transferInvoice.TotalAmount, transferEntry.EntryItems.Single(item => item.Account.AccountNumber == "2105").Debit);
        Assert.Equal(transferInvoice.TotalAmount, transferEntry.EntryItems.Single(item => item.Account.AccountNumber == "1102").Credit);
        Assert.DoesNotContain(await context.CashMovements.ToListAsync(), movement => movement.ReferenceId == transferRefund.Id);

        foreach (var request in concurrentRequests)
            request.Dispose();
        foreach (var response in concurrentResponses)
            response.Dispose();
    }

    private static async Task AssertCardSettlementLifecycleAsync(
        HttpClient client,
        IServiceProvider services,
        string accessToken,
        Guid caiId,
        Guid creditAuthorizationId)
    {
        using var invoiceRequest = InvoiceRequest(
            accessToken,
            caiId,
            Guid.NewGuid().ToString("D"));
        using var invoiceResponse = await client.SendAsync(invoiceRequest);
        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;

        using var paymentRequest = PaymentRequest(
            accessToken,
            Guid.NewGuid().ToString("D"),
            PaymentMethod.Tarjeta.ToString(),
            invoice.TotalAmount,
            invoice.Id,
            externalReference: "POS-VENTA-LIQUIDACION");
        using var paymentResponse = await client.SendAsync(paymentRequest);
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentDto>())!;

        using var eligibleRequest = Authorized(HttpMethod.Get, "/api/card-settlements/eligible-payments", accessToken);
        using var eligibleResponse = await client.SendAsync(eligibleRequest);
        Assert.Equal(HttpStatusCode.OK, eligibleResponse.StatusCode);
        var eligible = (await eligibleResponse.Content.ReadFromJsonAsync<List<EligibleCardPaymentDto>>())!;
        var eligiblePayment = Assert.Single(eligible, candidate => candidate.Id == payment.Id);
        Assert.Equal(invoice.TotalAmount, eligiblePayment.AvailableAmount);

        using var missingKeyRequest = CardSettlementRequest(
            accessToken,
            null,
            "LOTE-SIN-CLAVE",
            invoice.TotalAmount,
            114m,
            3m,
            2m,
            payment.Id);
        using var missingKeyResponse = await client.SendAsync(missingKeyRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingKeyResponse.StatusCode);

        using var invalidComponentsRequest = CardSettlementRequest(
            accessToken,
            Guid.NewGuid().ToString("D"),
            "LOTE-DESCUADRADO",
            invoice.TotalAmount,
            113m,
            3m,
            2m,
            payment.Id);
        using var invalidComponentsResponse = await client.SendAsync(invalidComponentsRequest);
        Assert.Equal(HttpStatusCode.BadRequest, invalidComponentsResponse.StatusCode);

        var keys = new[] { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") };
        var references = new[] { "LOTE-ADQUIRENTE-A", "LOTE-ADQUIRENTE-B" };
        var concurrentRequests = Enumerable.Range(0, 2)
            .Select(index => CardSettlementRequest(
                accessToken,
                keys[index],
                references[index],
                invoice.TotalAmount,
                114m,
                3m,
                2m,
                payment.Id))
            .ToArray();
        var concurrentResponses = await Task.WhenAll(concurrentRequests.Select(client.SendAsync));
        var concurrentDetails = await Task.WhenAll(concurrentResponses.Select(async response =>
            $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}"));
        Assert.True(
            concurrentResponses.Count(response => response.StatusCode == HttpStatusCode.Created) == 1,
            string.Join(Environment.NewLine, concurrentDetails));
        Assert.True(
            concurrentResponses.Count(response => response.StatusCode == HttpStatusCode.Conflict) == 1,
            string.Join(Environment.NewLine, concurrentDetails));
        var successfulIndex = Array.FindIndex(
            concurrentResponses,
            response => response.StatusCode == HttpStatusCode.Created);
        var settlement = (await concurrentResponses[successfulIndex]
            .Content.ReadFromJsonAsync<CardSettlementDto>())!;
        Assert.Equal(invoice.TotalAmount, settlement.GrossAmount);
        Assert.Equal(114m, settlement.BankDepositAmount);
        Assert.Equal(3m, settlement.CommissionAmount);
        Assert.Equal(2m, settlement.WithholdingAmount);
        Assert.Equal(references[successfulIndex], settlement.ExternalReference);
        Assert.Single(settlement.Applications);
        Assert.Equal(payment.Id, settlement.Applications[0].PaymentId);

        using var repeatedRequest = CardSettlementRequest(
            accessToken,
            keys[successfulIndex],
            references[successfulIndex],
            invoice.TotalAmount,
            114m,
            3m,
            2m,
            payment.Id);
        using var repeatedResponse = await client.SendAsync(repeatedRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedResponse.StatusCode);
        Assert.Equal(
            settlement.Id,
            (await repeatedResponse.Content.ReadFromJsonAsync<CardSettlementDto>())!.Id);

        using var conflictingRequest = CardSettlementRequest(
            accessToken,
            keys[successfulIndex],
            references[successfulIndex],
            invoice.TotalAmount,
            113m,
            4m,
            2m,
            payment.Id);
        using var conflictingResponse = await client.SendAsync(conflictingRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictingResponse.StatusCode);

        using var emptyEligibleRequest = Authorized(HttpMethod.Get, "/api/card-settlements/eligible-payments", accessToken);
        using var emptyEligibleResponse = await client.SendAsync(emptyEligibleRequest);
        Assert.Equal(HttpStatusCode.OK, emptyEligibleResponse.StatusCode);
        var remainingEligible = (await emptyEligibleResponse.Content.ReadFromJsonAsync<List<EligibleCardPaymentDto>>())!;
        Assert.DoesNotContain(remainingEligible, candidate => candidate.Id == payment.Id);

        using var listRequest = Authorized(HttpMethod.Get, "/api/card-settlements", accessToken);
        using var listResponse = await client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var settlements = (await listResponse.Content.ReadFromJsonAsync<List<CardSettlementDto>>())!;
        Assert.Contains(settlements, candidate => candidate.Id == settlement.Id);

        using var paymentDetailRequest = Authorized(HttpMethod.Get, $"/api/payments/{payment.Id}", accessToken);
        using var paymentDetailResponse = await client.SendAsync(paymentDetailRequest);
        Assert.Equal(HttpStatusCode.OK, paymentDetailResponse.StatusCode);
        Assert.Equal(
            invoice.TotalAmount,
            (await paymentDetailResponse.Content.ReadFromJsonAsync<PaymentDto>())!.CardSettledAmount);

        using var creditRequest = CreditNoteRequest(
            accessToken,
            invoice,
            creditAuthorizationId,
            Guid.NewGuid().ToString("D"));
        using var creditResponse = await client.SendAsync(creditRequest);
        Assert.Equal(HttpStatusCode.Created, creditResponse.StatusCode);
        var creditNote = (await creditResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;

        using var settledRefundRequest = RefundRequest(
            accessToken,
            payment.Id,
            Guid.NewGuid().ToString("D"),
            invoice.TotalAmount,
            creditNote.Id,
            null,
            "Reembolso posterior a liquidación",
            externalReference: "REVERSO-ADQUIRENTE");
        using var settledRefundResponse = await client.SendAsync(settledRefundRequest);
        Assert.Equal(HttpStatusCode.Conflict, settledRefundResponse.StatusCode);

        using var assertScope = services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await context.CardSettlements
            .Include(candidate => candidate.Applications)
            .SingleAsync(candidate => candidate.Id == settlement.Id);
        Assert.Equal(CardSettlementStatus.Confirmado, persisted.Status);
        Assert.Single(persisted.Applications);
        Assert.Equal(invoice.TotalAmount, persisted.Applications.Single().Amount);

        var entry = await context.AccountingEntries
            .Include(candidate => candidate.EntryItems)
            .ThenInclude(item => item.Account)
            .SingleAsync(candidate => candidate.ReferenceId == settlement.Id);
        Assert.Equal(entry.EntryItems.Sum(item => item.Debit), entry.EntryItems.Sum(item => item.Credit));
        Assert.Equal(114m, entry.EntryItems.Single(item => item.Account.AccountNumber == "1102").Debit);
        Assert.Equal(3m, entry.EntryItems.Single(item => item.Account.AccountNumber == "5201").Debit);
        Assert.Equal(2m, entry.EntryItems.Single(item => item.Account.AccountNumber == "1105").Debit);
        Assert.Equal(invoice.TotalAmount, entry.EntryItems.Single(item => item.Account.AccountNumber == "1104").Credit);
        Assert.DoesNotContain(await context.CashMovements.ToListAsync(), movement => movement.ReferenceId == settlement.Id);
        Assert.Single(await context.AuditLogs
            .Where(log => log.Action == "CreateCardSettlement" && log.EntityId == settlement.Id)
            .ToListAsync());
        Assert.Single(await context.IdempotencyRecords
            .Where(record => record.Scope == "card-settlement:create" && record.ResourceId == settlement.Id)
            .ToListAsync());

        foreach (var request in concurrentRequests)
            request.Dispose();
        foreach (var response in concurrentResponses)
            response.Dispose();
    }

    private static async Task AssertReservationLifecycleAsync(
        HttpClient client,
        IServiceProvider services,
        string accessToken)
    {
        var roomId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var checkInDate = HondurasToday().AddDays(60);
        var checkOutDate = checkInDate.AddDays(2);

        using (var seedScope = services.CreateScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roomType = new RoomType
            {
                Id = Guid.NewGuid(),
                Name = "Ciclo de reserva",
                PricePerNight = 150m,
                Capacity = 3
            };
            context.Rooms.Add(new Room
            {
                Id = roomId,
                RoomNumber = $"RS-{Guid.NewGuid():N}"[..12],
                Floor = 3,
                RoomType = roomType,
                Status = RoomStatus.Libre
            });
            context.Guests.Add(new Guest
            {
                Id = guestId,
                FirstName = "Huésped",
                LastName = "Reserva",
                DocumentNumber = $"RS-{Guid.NewGuid():N}"
            });
            await context.SaveChangesAsync();
        }

        var reservationPayload = new
        {
            guestId,
            roomId,
            checkInDate,
            checkOutDate,
            adults = 2,
            children = 0,
            advancePayment = 0m
        };

        using var advanceRequest = Authorized(HttpMethod.Post, "/api/reservations", accessToken);
        advanceRequest.Content = JsonContent.Create(new
        {
            guestId,
            roomId,
            checkInDate,
            checkOutDate,
            adults = 2,
            children = 0,
            advancePayment = 10m
        });
        using var advanceResponse = await client.SendAsync(advanceRequest);
        Assert.Equal(HttpStatusCode.BadRequest, advanceResponse.StatusCode);

        using var firstCreateRequest = Authorized(HttpMethod.Post, "/api/reservations", accessToken);
        firstCreateRequest.Content = JsonContent.Create(reservationPayload);
        using var secondCreateRequest = Authorized(HttpMethod.Post, "/api/reservations", accessToken);
        secondCreateRequest.Content = JsonContent.Create(reservationPayload);
        var createResponses = await Task.WhenAll(
            client.SendAsync(firstCreateRequest),
            client.SendAsync(secondCreateRequest));
        Assert.Contains(createResponses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Contains(createResponses, response => response.StatusCode == HttpStatusCode.Conflict);

        var createdResponse = createResponses.Single(response => response.StatusCode == HttpStatusCode.Created);
        var reservation = (await createdResponse.Content.ReadFromJsonAsync<ReservationDto>())!;
        Assert.Equal(1, reservation.Version);

        using var wrongConfirmRequest = Authorized(
            HttpMethod.Post,
            $"/api/reservations/{reservation.Id}/confirm",
            accessToken);
        wrongConfirmRequest.Content = JsonContent.Create(new { expectedVersion = 2 });
        using var wrongConfirmResponse = await client.SendAsync(wrongConfirmRequest);
        Assert.Equal(HttpStatusCode.Conflict, wrongConfirmResponse.StatusCode);

        using var confirmRequest = Authorized(
            HttpMethod.Post,
            $"/api/reservations/{reservation.Id}/confirm",
            accessToken);
        confirmRequest.Content = JsonContent.Create(new { expectedVersion = 1 });
        using var confirmResponse = await client.SendAsync(confirmRequest);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        using var staleUpdateRequest = Authorized(
            HttpMethod.Put,
            $"/api/reservations/{reservation.Id}",
            accessToken);
        staleUpdateRequest.Content = JsonContent.Create(new
        {
            notes = "Cambio obsoleto",
            expectedVersion = 1
        });
        using var staleUpdateResponse = await client.SendAsync(staleUpdateRequest);
        Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);

        using var staleCancelRequest = Authorized(
            HttpMethod.Post,
            $"/api/reservations/{reservation.Id}/cancel",
            accessToken);
        staleCancelRequest.Content = JsonContent.Create(new
        {
            expectedVersion = 1,
            reason = "Versión obsoleta"
        });
        using var staleCancelResponse = await client.SendAsync(staleCancelRequest);
        Assert.Equal(HttpStatusCode.Conflict, staleCancelResponse.StatusCode);

        using var cancelRequest = Authorized(
            HttpMethod.Post,
            $"/api/reservations/{reservation.Id}/cancel",
            accessToken);
        cancelRequest.Content = JsonContent.Create(new
        {
            expectedVersion = 2,
            reason = "Cambio de itinerario del huésped"
        });
        using var cancelResponse = await client.SendAsync(cancelRequest);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        using var repeatedCancelRequest = Authorized(
            HttpMethod.Post,
            $"/api/reservations/{reservation.Id}/cancel",
            accessToken);
        repeatedCancelRequest.Content = JsonContent.Create(new
        {
            expectedVersion = 2,
            reason = "Reintento de la misma operación"
        });
        using var repeatedCancelResponse = await client.SendAsync(repeatedCancelRequest);
        Assert.Equal(HttpStatusCode.OK, repeatedCancelResponse.StatusCode);

        using var confirmCancelledRequest = Authorized(
            HttpMethod.Post,
            $"/api/reservations/{reservation.Id}/confirm",
            accessToken);
        confirmCancelledRequest.Content = JsonContent.Create(new { expectedVersion = 3 });
        using var confirmCancelledResponse = await client.SendAsync(confirmCancelledRequest);
        Assert.Equal(HttpStatusCode.Conflict, confirmCancelledResponse.StatusCode);

        using var cancelledCheckInRequest = Authorized(
            HttpMethod.Post,
            "/api/reservations/checkin",
            accessToken);
        cancelledCheckInRequest.Content = JsonContent.Create(new
        {
            reservationId = reservation.Id,
            roomId,
            expectedVersion = 3,
            discountIds = Array.Empty<Guid>()
        });
        using var cancelledCheckInResponse = await client.SendAsync(cancelledCheckInRequest);
        Assert.Equal(HttpStatusCode.Conflict, cancelledCheckInResponse.StatusCode);

        using var deleteRequest = Authorized(
            HttpMethod.Delete,
            $"/api/reservations/{reservation.Id}",
            accessToken);
        using var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        using var assertScope = services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedReservation = await assertContext.Reservations.SingleAsync(candidate => candidate.Id == reservation.Id);
        var persistedRoom = await assertContext.Rooms.SingleAsync(candidate => candidate.Id == roomId);
        Assert.Equal(ReservationStatus.Cancelada, persistedReservation.Status);
        Assert.Equal(3, persistedReservation.Version);
        Assert.Equal(RoomStatus.Libre, persistedRoom.Status);
        Assert.Single(await assertContext.AuditLogs
            .Where(log => log.Action == "ConfirmReservation" && log.EntityId == reservation.Id)
            .ToListAsync());
        var cancellationAudit = await assertContext.AuditLogs
            .SingleAsync(log => log.Action == "CancelReservation" && log.EntityId == reservation.Id);
        Assert.Contains("Cambio de itinerario", cancellationAudit.Changes);

        foreach (var response in createResponses)
            response.Dispose();
    }

    private static async Task AssertConcurrentCheckInAndAtomicCheckoutAsync(
        HttpClient client,
        IServiceProvider services,
        string accessToken,
        Guid caiId)
    {
        var reservationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        int invoicesBefore;
        int entriesBefore;

        using (var seedScope = services.CreateScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            invoicesBefore = await context.Invoices.CountAsync();
            entriesBefore = await context.AccountingEntries.CountAsync();

            var roomType = new RoomType
            {
                Id = Guid.NewGuid(),
                Name = "Check-in integración",
                PricePerNight = 119m,
                Capacity = 2
            };
            var room = new Room
            {
                Id = roomId,
                RoomNumber = $"CI-{Guid.NewGuid():N}"[..12],
                Floor = 2,
                RoomType = roomType,
                Status = RoomStatus.Libre
            };
            var guest = new Guest
            {
                Id = Guid.NewGuid(),
                FirstName = "Huésped",
                LastName = "Check-in",
                DocumentNumber = $"CI-{Guid.NewGuid():N}"
            };
            context.Reservations.Add(new Reservation
            {
                Id = reservationId,
                Guest = guest,
                Room = room,
                CheckInDate = HondurasToday(),
                CheckOutDate = HondurasToday().AddDays(1),
                Adults = 1,
                Children = 0,
                Status = ReservationStatus.Confirmada
            });
            await context.SaveChangesAsync();
        }

        using var firstRequest = Authorized(HttpMethod.Post, "/api/reservations/checkin", accessToken);
        firstRequest.Content = JsonContent.Create(new
        {
            reservationId,
            roomId,
            expectedVersion = 1,
            discountIds = Array.Empty<Guid>()
        });
        using var secondRequest = Authorized(HttpMethod.Post, "/api/reservations/checkin", accessToken);
        secondRequest.Content = JsonContent.Create(new
        {
            reservationId,
            roomId,
            expectedVersion = 1,
            discountIds = Array.Empty<Guid>()
        });

        var responses = await Task.WhenAll(
            client.SendAsync(firstRequest),
            client.SendAsync(secondRequest));
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        using var assertScope = services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedReservation = await assertContext.Reservations.SingleAsync(candidate => candidate.Id == reservationId);
        var persistedRoom = await assertContext.Rooms.SingleAsync(candidate => candidate.Id == roomId);
        var persistedFolio = await assertContext.Folios
            .Include(folio => folio.FolioItems)
            .SingleAsync(folio => folio.ReservationId == reservationId);

        Assert.Equal(ReservationStatus.CheckIn, persistedReservation.Status);
        Assert.Equal(2, persistedReservation.Version);
        Assert.Equal(RoomStatus.Ocupada, persistedRoom.Status);
        Assert.Equal(FolioStatus.Abierto, persistedFolio.Status);
        Assert.Equal(119m, persistedFolio.TotalAmount);
        Assert.Single(persistedFolio.FolioItems);
        Assert.Equal(invoicesBefore, await assertContext.Invoices.CountAsync());
        Assert.Equal(entriesBefore, await assertContext.AccountingEntries.CountAsync());
        Assert.Single(await assertContext.AuditLogs
            .Where(log => log.Action == "CheckIn" && log.EntityId == reservationId)
            .ToListAsync());

        using var folioRequest = Authorized(
            HttpMethod.Get,
            $"/api/folios/by-reservation/{reservationId}",
            accessToken);
        using var folioResponse = await client.SendAsync(folioRequest);
        Assert.Equal(HttpStatusCode.OK, folioResponse.StatusCode);
        var folioDto = (await folioResponse.Content.ReadFromJsonAsync<FolioDto>())!;
        Assert.Single(folioDto.Items);
        Assert.Equal(119m, folioDto.TotalAmount);

        var userId = await assertContext.Users
            .Where(user => user.Username == "integration_admin")
            .Select(user => user.Id)
            .SingleAsync();
        var cashRegister = new CashRegister
        {
            Id = Guid.NewGuid(),
            Name = $"Caja CI {Guid.NewGuid():N}"[..20],
            IsActive = true
        };
        assertContext.CashRegisters.Add(cashRegister);
        assertContext.CashMovements.Add(new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegister = cashRegister,
            UserId = userId,
            MovementType = CashMovementType.Apertura,
            Amount = 50m,
            BalanceAfter = 50m,
            Description = "Apertura sintética"
        });
        await assertContext.SaveChangesAsync();

        using var addChargeRequest = Authorized(
            HttpMethod.Post,
            $"/api/folios/{persistedFolio.Id}/items",
            accessToken);
        addChargeRequest.Content = JsonContent.Create(new
        {
            description = "Minibar sintético",
            quantity = 2,
            unitPrice = 10m,
            isExempt = false,
            isvRate = 0.15m,
            isTouristTaxable = false,
            discountPercentage = 0m
        });
        using var addChargeResponse = await client.SendAsync(addChargeRequest);
        Assert.Equal(HttpStatusCode.OK, addChargeResponse.StatusCode);

        using var unbilledCheckoutRequest = Authorized(
            HttpMethod.Post,
            $"/api/reservations/checkout/{reservationId}",
            accessToken);
        unbilledCheckoutRequest.Content = JsonContent.Create(new { invoiceId = Guid.NewGuid() });
        using var unbilledCheckoutResponse = await client.SendAsync(unbilledCheckoutRequest);
        Assert.Equal(HttpStatusCode.Conflict, unbilledCheckoutResponse.StatusCode);

        var settlementKey = Guid.NewGuid().ToString("D");
        using var settlementRequest = FolioSettlementRequest(
            accessToken,
            caiId,
            persistedFolio.Id,
            persistedFolio.GuestId,
            cashRegister.Id,
            settlementKey);
        using var settlementResponse = await client.SendAsync(settlementRequest);
        Assert.Equal(HttpStatusCode.Created, settlementResponse.StatusCode);
        var settlementInvoice = (await settlementResponse.Content.ReadFromJsonAsync<InvoiceDto>())!;
        Assert.Equal(persistedFolio.Id, settlementInvoice.FolioId);
        Assert.Equal(142m, settlementInvoice.TotalAmount);
        Assert.Equal(150m, settlementInvoice.CashReceived);
        Assert.Equal(8m, settlementInvoice.CashChange);
        Assert.Equal(142m, settlementInvoice.PaidAmount);
        Assert.Equal(0m, settlementInvoice.BalanceDue);
        Assert.Equal(2, settlementInvoice.Items.Count);

        using var repeatedSettlementRequest = FolioSettlementRequest(
            accessToken,
            caiId,
            persistedFolio.Id,
            persistedFolio.GuestId,
            cashRegister.Id,
            settlementKey);
        using var repeatedSettlementResponse = await client.SendAsync(repeatedSettlementRequest);
        Assert.Equal(HttpStatusCode.Created, repeatedSettlementResponse.StatusCode);
        Assert.Equal(
            settlementInvoice.Id,
            (await repeatedSettlementResponse.Content.ReadFromJsonAsync<InvoiceDto>())!.Id);

        using var duplicateSettlementRequest = FolioSettlementRequest(
            accessToken,
            caiId,
            persistedFolio.Id,
            persistedFolio.GuestId,
            cashRegister.Id,
            Guid.NewGuid().ToString("D"));
        using var duplicateSettlementResponse = await client.SendAsync(duplicateSettlementRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateSettlementResponse.StatusCode);

        assertContext.ChangeTracker.Clear();
        persistedReservation = await assertContext.Reservations.SingleAsync(candidate => candidate.Id == reservationId);
        persistedRoom = await assertContext.Rooms.SingleAsync(candidate => candidate.Id == roomId);
        persistedFolio = await assertContext.Folios
            .Include(folio => folio.FolioItems)
            .SingleAsync(folio => folio.ReservationId == reservationId);
        var persistedInvoice = await assertContext.Invoices
            .Include(invoice => invoice.InvoiceItems)
            .Include(invoice => invoice.PaymentApplications)
            .ThenInclude(application => application.Payment)
            .AsSplitQuery()
            .SingleAsync(invoice => invoice.FolioId == persistedFolio.Id);
        var settlementPayment = persistedInvoice.PaymentApplications.Single().Payment;
        var cashIncome = await assertContext.CashMovements
            .SingleAsync(movement => movement.ReferenceId == settlementPayment.Id);

        Assert.Equal(ReservationStatus.CheckOut, persistedReservation.Status);
        Assert.Equal(3, persistedReservation.Version);
        Assert.Equal(RoomStatus.Limpieza, persistedRoom.Status);
        Assert.Equal(FolioStatus.Cerrado, persistedFolio.Status);
        Assert.NotNull(persistedFolio.ClosingDate);
        Assert.Equal(142m, persistedFolio.TotalAmount);
        Assert.Equal(2, persistedFolio.FolioItems.Count);
        Assert.Equal(
            persistedFolio.FolioItems.Select(item => item.Id).Order(),
            persistedInvoice.InvoiceItems.Select(item => item.FolioItemId!.Value).Order());
        Assert.Equal(CashMovementType.Ingreso, cashIncome.MovementType);
        Assert.Equal(142m, cashIncome.Amount);
        Assert.Equal(192m, cashIncome.BalanceAfter);
        Assert.Equal(invoicesBefore + 1, await assertContext.Invoices.CountAsync());
        Assert.Equal(entriesBefore + 2, await assertContext.AccountingEntries.CountAsync());
        Assert.Equal(PaymentMethod.Efectivo, settlementPayment.Method);
        Assert.Equal(142m, settlementPayment.Amount);
        Assert.Equal(150m, settlementPayment.CashReceived);
        Assert.Equal(8m, settlementPayment.CashChange);
        Assert.NotNull(settlementPayment.AccountingEntryId);
        Assert.Single(await assertContext.AuditLogs
            .Where(log => log.Action == "CheckOut" && log.EntityId == reservationId)
            .ToListAsync());
        Assert.Single(await assertContext.AuditLogs
            .Where(log => log.Action == "AddFolioCharge" && log.EntityId == persistedFolio.Id)
            .ToListAsync());
        await AssertInvoiceAccountingAndAuditAsync(services, persistedInvoice.Id, 142m, expectedPaid: true);
    }

    private static async Task<AuthResponse> LoginAsync(
        HttpClient client,
        string password,
        string username = "integration_admin")
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Null(session.RefreshToken);
        session.RefreshToken = GetSetCookieValue(response, "hotel_erp_refresh");
        return session;
    }

    private static string GetSetCookieHeader(HttpResponseMessage response, string cookieName)
        => response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{cookieName}=", StringComparison.Ordinal));

    private static string GetSetCookieValue(HttpResponseMessage response, string cookieName)
    {
        var header = GetSetCookieHeader(response, cookieName);
        var separatorIndex = header.IndexOf(';');
        var pair = separatorIndex >= 0 ? header[..separatorIndex] : header;
        return Uri.UnescapeDataString(pair[(cookieName.Length + 1)..]);
    }

    private static async Task AssertCorsSessionPolicyAsync(HttpClient client)
    {
        using var allowedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/auth/refresh");
        allowedRequest.Headers.Add("Origin", "http://localhost");
        allowedRequest.Headers.Add("Access-Control-Request-Method", "POST");
        allowedRequest.Headers.Add("Access-Control-Request-Headers", "X-CSRF-Token");
        using var allowedResponse = await client.SendAsync(allowedRequest);
        Assert.Equal(HttpStatusCode.NoContent, allowedResponse.StatusCode);
        Assert.Equal("http://localhost", allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", allowedResponse.Headers.GetValues("Access-Control-Allow-Credentials").Single());

        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/auth/refresh");
        rejectedRequest.Headers.Add("Origin", "https://malicious.example");
        rejectedRequest.Headers.Add("Access-Control-Request-Method", "POST");
        rejectedRequest.Headers.Add("Access-Control-Request-Headers", "X-CSRF-Token");
        using var rejectedResponse = await client.SendAsync(rejectedRequest);
        Assert.False(rejectedResponse.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(rejectedResponse.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static HttpRequestMessage CashOperationRequest(
        string accessToken,
        Guid cashRegisterId,
        string operation,
        string idempotencyKey,
        object body)
    {
        var request = Authorized(
            HttpMethod.Post,
            $"/api/cash-registers/{cashRegisterId}/{operation}",
            accessToken);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(body);
        return request;
    }

    private static HttpRequestMessage PaymentRequest(
        string accessToken,
        string? idempotencyKey,
        string method,
        decimal amount,
        Guid invoiceId,
        Guid? cashRegisterId = null,
        decimal? cashReceived = null,
        string? externalReference = null)
    {
        var request = Authorized(HttpMethod.Post, "/api/payments", accessToken);
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            method,
            currency = "HNL",
            amount,
            cashReceived,
            cashRegisterId,
            externalReference,
            applications = new[]
            {
                new { invoiceId, amount }
            }
        });
        return request;
    }

    private static HttpRequestMessage RefundRequest(
        string accessToken,
        Guid paymentId,
        string? idempotencyKey,
        decimal amount,
        Guid creditNoteId,
        Guid? cashRegisterId,
        string reason,
        string? externalReference = null)
    {
        var request = Authorized(
            HttpMethod.Post,
            $"/api/payments/{paymentId}/refunds",
            accessToken);
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            amount,
            cashRegisterId,
            externalReference,
            reason,
            applications = new[]
            {
                new { creditNoteId, amount }
            }
        });
        return request;
    }

    private static HttpRequestMessage CardSettlementRequest(
        string accessToken,
        string? idempotencyKey,
        string externalReference,
        decimal grossAmount,
        decimal bankDepositAmount,
        decimal commissionAmount,
        decimal withholdingAmount,
        Guid paymentId)
    {
        var request = Authorized(HttpMethod.Post, "/api/card-settlements", accessToken);
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            currency = "HNL",
            grossAmount,
            bankDepositAmount,
            commissionAmount,
            withholdingAmount,
            externalReference,
            settlementDate = HondurasToday(),
            applications = new[]
            {
                new { paymentId, amount = grossAmount }
            }
        });
        return request;
    }

    private static HttpRequestMessage InvoiceRequest(
        string accessToken,
        Guid? caiId = null,
        string? idempotencyKey = null,
        decimal unitPrice = 100m)
    {
        var request = Authorized(HttpMethod.Post, "/api/invoices", accessToken);
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            caiId = caiId ?? Guid.NewGuid(),
            customerName = "Cliente de integración",
            documentType = "Factura",
            taxpayerType = "ConsumidorFinal",
            items = new[]
            {
                new
                {
                    description = "Hospedaje",
                    quantity = 1,
                    unitPrice,
                    lineTotal = unitPrice,
                    isExempt = false,
                    isvRate = 0.15m,
                    isTouristTaxable = true,
                    discountPercentage = 0m
                }
            }
        });
        return request;
    }

    private static HttpRequestMessage FolioSettlementRequest(
        string accessToken,
        Guid caiId,
        Guid folioId,
        Guid guestId,
        Guid cashRegisterId,
        string idempotencyKey)
    {
        var request = Authorized(HttpMethod.Post, "/api/invoices", accessToken);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            caiId,
            folioId,
            guestId,
            customerName = "Huésped Check-in",
            taxpayerType = "ConsumidorFinal",
            paymentMethod = "Efectivo",
            cashRegisterId,
            cashReceived = 150m
        });
        return request;
    }

    private static HttpRequestMessage DebitNoteRequest(
        string accessToken,
        InvoiceDto original,
        Guid authorizationId,
        string idempotencyKey)
    {
        var request = Authorized(HttpMethod.Post, $"/api/invoices/{original.Id}/debit-note", accessToken);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            caiId = original.CAIId,
            documentAuthorizationId = authorizationId,
            originalInvoiceId = original.Id,
            reason = "Recargo sintético omitido en la factura",
            items = new[]
            {
                new
                {
                    description = "Recargo sintético",
                    quantity = 1,
                    unitPrice = 10m,
                    isExempt = false,
                    isvRate = 0.15m,
                    isTouristTaxable = false,
                    discountPercentage = 0m
                }
            }
        });
        return request;
    }

    private static HttpRequestMessage CreditNoteRequest(
        string accessToken,
        InvoiceDto original,
        Guid authorizationId,
        string idempotencyKey)
    {
        var request = Authorized(HttpMethod.Post, $"/api/invoices/{original.Id}/credit-note", accessToken);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            caiId = original.CAIId,
            documentAuthorizationId = authorizationId,
            originalInvoiceId = original.Id,
            reason = "Reversión sintética total",
            items = new[]
            {
                new
                {
                    originalInvoiceItemId = original.Items.Single().Id,
                    quantity = original.Items.Single().Quantity
                }
            }
        });
        return request;
    }

    private static DateOnly HondurasToday()
        => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));

    private static async Task<Guid> CreateActiveCaiAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cai = new CAI
        {
            Id = Guid.NewGuid(),
            CAINumber = $"CAI-INTEGRATION-{Guid.NewGuid():N}",
            IssueDate = HondurasToday().AddDays(-1),
            DueDate = HondurasToday().AddDays(30),
            InitialRange = "001-001-01-00000001",
            FinalRange = "001-001-01-00000100",
            CurrentCorrelative = "001-001-01-00000001",
            Status = CAIStatus.Activo
        };
        context.CAIs.Add(cai);
        await context.SaveChangesAsync();
        return cai.Id;
    }

    private static async Task<Guid> CreateDocumentAuthorizationAsync(
        HttpClient client,
        string accessToken,
        InvoiceDocumentType documentType,
        string initialRange)
    {
        using var request = Authorized(HttpMethod.Post, "/api/document-authorizations", accessToken);
        request.Content = JsonContent.Create(new
        {
            documentType = documentType.ToString(),
            caiNumber = $"CAI-{documentType}-{Guid.NewGuid():N}",
            issueDate = HondurasToday().AddDays(-1),
            dueDate = HondurasToday().AddDays(30),
            initialRange,
            finalRange = initialRange[..^8] + "00000100"
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var authorization = (await response.Content.ReadFromJsonAsync<DocumentAuthorizationDto>())!;
        Assert.Equal(HondurasToday().AddDays(30), authorization.DueDate);
        return authorization.Id;
    }

    private static async Task AssertInvoiceAccountingAndAuditAsync(
        IServiceProvider services,
        Guid invoiceId,
        decimal expectedTotal = 119m,
        bool expectedPaid = false)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invoice = await context.Invoices
            .Include(candidate => candidate.PaymentApplications)
            .ThenInclude(application => application.Payment)
            .SingleAsync(candidate => candidate.Id == invoiceId);
        Assert.Equal(expectedTotal, invoice.TotalAmount);
        Assert.NotEmpty(invoice.FiscalHash!);
        if (expectedPaid)
        {
            Assert.Equal(InvoiceStatus.Pagada, invoice.Status);
            Assert.Equal("Efectivo", invoice.PaymentMethod);
            Assert.Equal(expectedTotal, invoice.PaymentApplications.Sum(application => application.Amount));
            Assert.Single(invoice.PaymentApplications);
        }
        else
        {
            Assert.Equal(InvoiceStatus.Emitida, invoice.Status);
            Assert.Null(invoice.PaymentMethod);
            Assert.Empty(invoice.PaymentApplications);
        }

        var accountingEntry = await context.AccountingEntries
            .Include(entry => entry.EntryItems)
            .ThenInclude(item => item.Account)
            .SingleAsync(entry => entry.ReferenceId == invoiceId);
        Assert.Equal(accountingEntry.EntryItems.Sum(item => item.Debit), accountingEntry.EntryItems.Sum(item => item.Credit));
        Assert.Equal(expectedTotal, accountingEntry.EntryItems.Single(item => item.Account.AccountNumber == "1103").Debit);
        Assert.Single(await context.AuditLogs
            .Where(log => log.Action == "CreateInvoice" && log.EntityId == invoiceId)
            .ToListAsync());
        Assert.Single(await context.IdempotencyRecords
            .Where(record => record.Scope == "invoice:create" && record.ResourceId == invoiceId)
            .ToListAsync());
    }

    private static async Task AssertAdjustmentAccountingAndAuditAsync(
        IServiceProvider services,
        Guid invoiceId,
        bool isCredit,
        bool expectedCustomerCredit = false)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invoice = await context.Invoices.SingleAsync(candidate => candidate.Id == invoiceId);
        Assert.NotEmpty(invoice.FiscalHash!);

        var entry = await context.AccountingEntries
            .Include(candidate => candidate.EntryItems)
            .ThenInclude(item => item.Account)
            .SingleAsync(candidate => candidate.ReferenceId == invoiceId);
        Assert.Equal(entry.EntryItems.Sum(item => item.Debit), entry.EntryItems.Sum(item => item.Credit));

        if (isCredit)
        {
            Assert.Equal(invoice.SubTotal, entry.EntryItems.Single(item => item.Account.AccountNumber == "4101").Debit);
            Assert.Equal(invoice.ISVAmount, entry.EntryItems.Single(item => item.Account.AccountNumber == "2101").Debit);
            Assert.Equal(invoice.TouristTaxAmount, entry.EntryItems.Single(item => item.Account.AccountNumber == "2102").Debit);
            if (expectedCustomerCredit)
            {
                Assert.Equal(invoice.TotalAmount, entry.EntryItems.Single(item => item.Account.AccountNumber == "2105").Credit);
                Assert.DoesNotContain(entry.EntryItems, item => item.Account.AccountNumber == "1103");
            }
            else
            {
                Assert.Equal(invoice.TotalAmount, entry.EntryItems.Single(item => item.Account.AccountNumber == "1103").Credit);
                Assert.DoesNotContain(entry.EntryItems, item => item.Account.AccountNumber == "2105");
            }
        }
        else
        {
            Assert.Equal(invoice.TotalAmount, entry.EntryItems.Single(item => item.Account.AccountNumber == "1103").Debit);
            Assert.Equal(invoice.SubTotal, entry.EntryItems.Single(item => item.Account.AccountNumber == "4101").Credit);
            Assert.Equal(invoice.ISVAmount, entry.EntryItems.Single(item => item.Account.AccountNumber == "2101").Credit);
        }

        var action = isCredit ? "CreateCreditNote" : "CreateDebitNote";
        Assert.Single(await context.AuditLogs
            .Where(log => log.Action == action && log.EntityId == invoiceId)
            .ToListAsync());
        Assert.Single(await context.IdempotencyRecords
            .Where(record => record.ResourceId == invoiceId)
            .ToListAsync());
    }

    private static async Task AssertSafeProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? expectedTitle = null)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        using var json = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal((int)expectedStatus, json.RootElement.GetProperty("status").GetInt32());
        if (expectedTitle is not null)
        {
            Assert.Equal(expectedTitle, json.RootElement.GetProperty("title").GetString());
        }

        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("unknown_operator", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Incorrect!", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("synthetic-database-failure", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingAuthService : IAuthService
    {
        public Task<AuthResponse> LoginAsync(LoginRequest request)
            => throw new InvalidOperationException("synthetic-database-failure: Npgsql stack detail");

        public Task<AuthResponse> RegisterAsync(RegisterRequest request) => throw new NotSupportedException();
        public Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request) => throw new NotSupportedException();
        public Task LogoutAsync(Guid userId) => throw new NotSupportedException();
        public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request) => throw new NotSupportedException();
        public Task ForgotPasswordAsync(ForgotPasswordRequest request) => throw new NotSupportedException();
        public Task ResetPasswordAsync(ResetPasswordRequest request) => throw new NotSupportedException();
    }
}
