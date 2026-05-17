using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using hotel_erp.Application.DTOs;
using hotel_erp.Application.Interfaces;
using hotel_erp.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace hotel_erp.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    }
                    await _userRepository.UpdateAsync(user);
                }
                return new AuthResponse { Success = false, Message = "Credenciales inválidas" };
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return new AuthResponse { Success = false, Message = $"Cuenta bloqueada hasta {user.LockoutEnd:HH:mm}" };
            }

            if (!user.IsActive)
            {
                return new AuthResponse { Success = false, Message = "Cuenta desactivada" };
            }

            user.FailedLoginAttempts = 0;
            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return await _jwtService.GenerateTokensAsync(user.Id);
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
                return new AuthResponse { Success = false, Message = "El usuario ya existe" };

            existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return new AuthResponse { Success = false, Message = "El email ya está registrado" };

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true
            };

            await _userRepository.AddAsync(user);

            // Assign default "Recepción" role
            return await _jwtService.GenerateTokensAsync(user.Id);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var userId = await _jwtService.ValidateRefreshTokenAsync(request.RefreshToken);
            if (userId == null)
                return new AuthResponse { Success = false, Message = "Token inválido o expirado" };

            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
            return await _jwtService.GenerateTokensAsync(userId.Value);
        }

        public async Task LogoutAsync(Guid userId)
        {
            // Revoke all refresh tokens for user
            // Implementation should be in the repository
            await Task.CompletedTask;
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("Usuario no encontrado");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Contraseña actual incorrecta");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);
        }

        public Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // Implement email sending logic
            throw new NotImplementedException("Feature coming soon");
        }

        public Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            throw new NotImplementedException("Feature coming soon");
        }
    }

    public class JwtService : IJwtService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public JwtService(IUserRepository userRepository, IMapper mapper, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<AuthResponse> GenerateTokensAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return new AuthResponse { Success = false, Message = "Usuario no encontrado" };

            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"]!;
            var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");

            var roles = await _userRepository.GetUserRolesAsync(userId);
            var permissions = await _userRepository.GetUserPermissionsAsync(userId);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("fullName", $"{user.FirstName} {user.LastName}")
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r.Name)));
            claims.AddRange(permissions.Select(p => new Claim("permission", p.Name)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();
            var refreshExpirationDays = int.Parse(jwtSettings["RefreshTokenExpirationInDays"] ?? "7");

            // Store refresh token
            var userEntity = await _userRepository.GetByIdAsync(userId);
            // In a real implementation, we'd use a refresh token repository
            // For now, we'll store it in the user's refresh tokens collection

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles.Select(r => r.Name).ToList();
            userDto.Permissions = permissions.Select(p => p.Name).ToList();

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expires,
                User = userDto
            };
        }

        public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken)
        {
            // TODO: Implement refresh token validation from DB
            await Task.CompletedTask;
            return null;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            await Task.CompletedTask;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
