using hotel_erp.Api.Dtos.Auth;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using hotel_erp.Api.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AuditService _auditService;
        private readonly AuthenticationSessionCookies _sessionCookies;

        public AuthController(
            IAuthService authService,
            AuditService auditService,
            AuthenticationSessionCookies sessionCookies)
        {
            _authService = authService;
            _auditService = auditService;
            _sessionCookies = sessionCookies;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthenticationRateLimitPolicyNames.Login)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            Response.Headers.CacheControl = "no-store";
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
            {
                return Problem(
                    type: "https://httpstatuses.com/401",
                    title: "Autenticación rechazada",
                    statusCode: StatusCodes.Status401Unauthorized,
                    detail: "Usuario o contraseña inválidos.");
            }
            _sessionCookies.Issue(Response, result.RefreshToken!);
            result.RefreshToken = null;
            await _auditService.LogAsync(result.User?.Id, "Login", null, null, request.Username);
            return Ok(result);
        }

        [Authorize(Policy = PermissionNames.ManageUsers)]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success)
                return BadRequest(result);
            var actorUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(actorUserId, "Register", nameof(User), result.User?.Id, request.Username);
            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthenticationRateLimitPolicyNames.Refresh)]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
        {
            Response.Headers.CacheControl = "no-store";
            var refreshToken = request.RefreshToken;
            var usesCookie = string.IsNullOrWhiteSpace(refreshToken);
            if (usesCookie)
            {
                refreshToken = _sessionCookies.ReadRefreshToken(Request);
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return SessionRejected();
                }

                if (!_sessionCookies.HasValidCsrfToken(Request))
                {
                    return Problem(
                        type: "https://httpstatuses.com/403",
                        title: "Solicitud de sesión rechazada",
                        statusCode: StatusCodes.Status403Forbidden,
                        detail: "La comprobación CSRF no es válida.");
                }
            }

            var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest(refreshToken));
            if (!result.Success || string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                _sessionCookies.Clear(Response);
                return SessionRejected();
            }

            _sessionCookies.Issue(Response, result.RefreshToken);
            result.RefreshToken = null;
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await _authService.LogoutAsync(userId);
                await _auditService.LogAsync(userId, "Logout", null, userId);
                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            finally
            {
                // El navegador no debe conservar una cookie utilizable si la revocación
                // o su auditoría fallan por una interrupción de base de datos.
                _sessionCookies.Clear(Response);
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        [EnableRateLimiting(AuthenticationRateLimitPolicyNames.ChangePassword)]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await _authService.ChangePasswordAsync(userId, request);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Problem(
                    type: "https://httpstatuses.com/400",
                    title: "No se pudo cambiar la contraseña",
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return Problem(
                    type: "https://httpstatuses.com/400",
                    title: "No se pudo cambiar la contraseña",
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: exception.Message);
            }

            await _auditService.LogAsync(userId, "ChangePassword", nameof(User), userId);
            _sessionCookies.Clear(Response);
            return Ok(new { message = "Contraseña cambiada exitosamente" });
        }

        [HttpGet("me")]
        [Authorize]
        public ActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var fullName = User.FindFirstValue("fullName");
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var permissions = User.FindAll("permission").Select(c => c.Value).ToList();
            var mustChangePassword = bool.TryParse(
                User.FindFirstValue(SecurityClaimTypes.MustChangePassword),
                out var changeRequired) && changeRequired;

            return Ok(new
            {
                Id = userId,
                Username = username,
                Email = email,
                FullName = fullName,
                Roles = roles,
                Permissions = permissions,
                MustChangePassword = mustChangePassword
            });
        }

        private ObjectResult SessionRejected()
            => Problem(
                type: "https://httpstatuses.com/401",
                title: "Sesión rechazada",
                statusCode: StatusCodes.Status401Unauthorized,
                detail: "La sesión no es válida o ya expiró.");
    }
}
