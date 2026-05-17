namespace hotel_erp.Application.DTOs
{
    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(string Username, string Password, string Email, string FirstName, string LastName);
    public record RefreshTokenRequest(string RefreshToken);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NewPassword);

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

    public record CreateRoleRequest(string Name, string? Description, List<string>? Permissions);
    public record UpdateRoleRequest(string? Name, string? Description, List<string>? Permissions);
    public record AssignRoleRequest(Guid UserId, Guid RoleId);
    public record AssignPermissionsRequest(Guid RoleId, List<string> PermissionNames);
}
