using hotel_erp.Api.Dtos.Auth;
using hotel_erp.Api.Dtos.Common;

namespace hotel_erp.Api.Services.Interfaces
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
        Task<Guid?> ConsumeRefreshTokenAsync(string refreshToken);
        Task RevokeAllRefreshTokensAsync(Guid userId);
    }
}


