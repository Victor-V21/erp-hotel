using hotel_erp.Api.Dtos.Common;
using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Auth
{
    public record LoginRequest(
        [Required, StringLength(50, MinimumLength = 3)] string Username,
        [Required, StringLength(100, MinimumLength = 6)] string Password);

    public record RegisterRequest(
        [Required, StringLength(50, MinimumLength = 3)] string Username,
        [Required, StringLength(100, MinimumLength = 8)] string Password,
        [Required, EmailAddress, StringLength(100)] string Email,
        [Required, StringLength(50, MinimumLength = 2)] string FirstName,
        [Required, StringLength(50, MinimumLength = 2)] string LastName);

    public record RefreshTokenRequest([Required] string RefreshToken);

    public record ChangePasswordRequest(
        [Required] string CurrentPassword,
        [Required, StringLength(100, MinimumLength = 8)] string NewPassword);

    public record ForgotPasswordRequest([Required, EmailAddress, StringLength(100)] string Email);

    public record ResetPasswordRequest(
        [Required, EmailAddress, StringLength(100)] string Email,
        [Required] string Token,
        [Required, StringLength(100, MinimumLength = 8)] string NewPassword);

    public record AuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserDto? User { get; set; }
    }

    public record UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public record RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public record PermissionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public record CreateRoleRequest(
        [Required, StringLength(50, MinimumLength = 3)] string Name,
        [StringLength(250)] string? Description,
        List<string>? Permissions);

    public record UpdateRoleRequest(
        [StringLength(50, MinimumLength = 3)] string? Name,
        [StringLength(250)] string? Description,
        List<string>? Permissions);

    public record AssignRoleRequest(
        [NotEmptyGuid] Guid UserId,
        [NotEmptyGuid] Guid RoleId);

    public record AssignPermissionsRequest(
        [NotEmptyGuid] Guid RoleId,
        [Required, MinLength(1)] List<string> PermissionNames);

    public record CreateUserRequest(
        [Required, StringLength(50, MinimumLength = 3)] string Username,
        [Required, StringLength(100, MinimumLength = 8)] string Password,
        [Required, EmailAddress, StringLength(100)] string Email,
        [Required, StringLength(50, MinimumLength = 2)] string FirstName,
        [Required, StringLength(50, MinimumLength = 2)] string LastName,
        List<string>? Roles);

    public record UpdateUserRequest(
        [StringLength(50, MinimumLength = 2)] string? FirstName,
        [StringLength(50, MinimumLength = 2)] string? LastName,
        [EmailAddress, StringLength(100)] string? Email,
        bool? IsActive,
        List<string>? Roles);

    public record AdminResetPasswordRequest(
        [Required, StringLength(100, MinimumLength = 8)] string NewPassword);
}



