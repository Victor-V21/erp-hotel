using hotel_erp.Application.DTOs;

namespace hotel_erp.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(Guid userId);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
    }

    public interface IJwtService
    {
        Task<AuthResponse> GenerateTokensAsync(Guid userId);
        Task<Guid?> ValidateRefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
