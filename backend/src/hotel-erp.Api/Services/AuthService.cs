using hotel_erp.Api.Dtos.Auth;
using hotel_erp.Api.Database.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IMapper mapper,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _mapper = mapper;
            _configuration = configuration;
            _context = context;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
                return new AuthResponse { Success = false, Message = "Credenciales inválidas" };

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                return new AuthResponse { Success = false, Message = $"Cuenta bloqueada hasta {user.LockoutEnd:HH:mm}" };

            if (!user.IsActive)
                return new AuthResponse { Success = false, Message = "Cuenta desactivada" };

            if (!IsValidPassword(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await _userRepository.UpdateAsync(user);
                return new AuthResponse { Success = false, Message = "Credenciales inválidas" };
            }

            user.FailedLoginAttempts = 0;
            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return await _jwtService.GenerateTokensAsync(user.Id);
        }

        private static bool IsValidPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
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

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Recepcion");
            if (defaultRole != null)
            {
                await _context.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = defaultRole.Id });
                await _context.SaveChangesAsync();
            }

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
            await _jwtService.RevokeAllRefreshTokensAsync(userId);
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("Usuario no encontrado");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Contraseña actual incorrecta");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);
            await _jwtService.RevokeAllRefreshTokensAsync(userId);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null) return;

            var tokenBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            var token = Convert.ToHexString(tokenBytes);

            await _context.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = $"RESET:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))}",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsRevoked = false
            });
            await _context.SaveChangesAsync();
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email)
                ?? throw new UnauthorizedAccessException("Usuario no encontrado");

            var tokenHash = $"RESET:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)))}";
            var resetToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == user.Id && rt.Token == tokenHash && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

            if (resetToken == null)
                throw new UnauthorizedAccessException("Token inválido o expirado");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            resetToken.IsRevoked = true;
            resetToken.RevokedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _context.SaveChangesAsync();
            await _jwtService.RevokeAllRefreshTokensAsync(user.Id);
        }
    }

    public class JwtService : IJwtService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public JwtService(IUserRepository userRepository, IMapper mapper, IConfiguration configuration, ApplicationDbContext context)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _configuration = configuration;
            _context = context;
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
            var refreshTokenHash = HashToken(refreshToken);
            var refreshExpirationDays = int.Parse(jwtSettings["RefreshTokenExpirationInDays"] ?? "7");

            await RevokeAllRefreshTokensAsync(userId);
            await _context.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = userId,
                Token = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshExpirationDays),
                IsRevoked = false
            });
            await _context.SaveChangesAsync();

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
            var tokenHash = HashToken(refreshToken);
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == tokenHash && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

            return token?.UserId;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == tokenHash);
            if (token == null) return;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllRefreshTokensAsync(Guid userId)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            if (activeTokens.Count == 0) return;

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static string HashToken(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}



