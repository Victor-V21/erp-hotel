using hotel_erp.Api.Dtos.Auth;
using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using hotel_erp.Api.Authorization;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = PermissionNames.ManageUsers)]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UsersController(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IJwtService jwtService,
            ApplicationDbContext context,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _jwtService = jwtService;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userRepository.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound("Usuario no encontrado");
            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
                return BadRequest(new { message = "El nombre de usuario ya está en uso" });

            existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new { message = "El correo electrónico ya está registrado" });

            // Assign roles if provided, otherwise default to "Recepcion"
            var roleNames = request.Roles != null && request.Roles.Any()
                ? request.Roles
                : new List<string> { "Recepcion" };

            var allRoles = await _context.Roles.Where(role => !role.IsDeleted).ToListAsync();
            var selectedRoles = roleNames
                .Select(roleName => allRoles.FirstOrDefault(role =>
                    role.NormalizedName == SecurityCatalogSeeder.NormalizeRoleName(roleName)))
                .ToList();

            if (selectedRoles.Any(role => role is null))
                return BadRequest(new { message = "Uno o más roles solicitados no existen" });

            if (!await CanAssignRolesAsync(selectedRoles.OfType<Role>()))
                return Forbid();

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                MustChangePassword = true
            };

            await _context.Users.AddAsync(user);

            foreach (var role in selectedRoles.OfType<Role>())
            {
                await _context.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
            }

            await _context.SaveChangesAsync();

            var createdUser = await _userRepository.GetByIdAsync(user.Id);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, _mapper.Map<UserDto>(createdUser));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound("Usuario no encontrado");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (request.IsActive == false && Guid.TryParse(currentUserId, out var actorId) && actorId == id)
                return BadRequest(new { message = "No puede desactivar su propia cuenta" });

            if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id);
                if (emailTaken)
                    return BadRequest(new { message = "El correo electrónico ya pertenece a otro usuario" });
                user.Email = request.Email.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName.Trim();

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (request.Roles != null)
            {
                var allRoles = await _context.Roles.Where(role => !role.IsDeleted).ToListAsync();
                var selectedRoles = request.Roles
                    .Select(roleName => allRoles.FirstOrDefault(role =>
                        role.NormalizedName == SecurityCatalogSeeder.NormalizeRoleName(roleName)))
                    .ToList();

                if (selectedRoles.Any(role => role is null))
                    return BadRequest(new { message = "Uno o más roles solicitados no existen" });

                if (!await CanAssignRolesAsync(selectedRoles.OfType<Role>()))
                    return Forbid();

                // Remove existing user roles
                _context.UserRoles.RemoveRange(user.UserRoles);

                foreach (var role in selectedRoles.OfType<Role>())
                {
                    await _context.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
            }

            user.SecurityVersion++;
            await _context.SaveChangesAsync();
            await _jwtService.RevokeAllRefreshTokensAsync(id);

            var updatedUser = await _userRepository.GetByIdAsync(id);
            return Ok(_mapper.Map<UserDto>(updatedUser));
        }

        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound("Usuario no encontrado");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = true;
            user.SecurityVersion++;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await _userRepository.UpdateAsync(user);
            await _jwtService.RevokeAllRefreshTokensAsync(id);

            return Ok(new { message = "Contraseña restablecida exitosamente" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != null && Guid.Parse(currentUserId) == id)
            {
                return BadRequest(new { message = "No puede eliminar o desactivar su propia cuenta de administrador" });
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound("Usuario no encontrado");

            var removesAdministrator = user.UserRoles.Any(userRole =>
                userRole.Role.SystemKey == SystemRoleKeys.Administrator);
            if (removesAdministrator)
            {
                var activeAdministrators = await _context.UserRoles.CountAsync(userRole =>
                    userRole.Role.SystemKey == SystemRoleKeys.Administrator
                    && userRole.User.IsActive
                    && !userRole.User.IsDeleted);
                if (activeAdministrators <= 1)
                    return BadRequest(new { message = "No puede desactivar al último administrador activo" });
            }

            user.IsActive = false;
            user.SecurityVersion++;
            await _userRepository.UpdateAsync(user);
            await _jwtService.RevokeAllRefreshTokensAsync(id);

            return Ok(new { message = "Usuario desactivado exitosamente" });
        }

        [HttpPost("{userId}/roles/{roleId}")]
        public async Task<ActionResult> AssignRole(Guid userId, Guid roleId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound("Usuario no encontrado");

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null) return NotFound("Rol no encontrado");

            if (!await CanAssignRolesAsync([role]))
                return Forbid();

            var userRoles = await _userRepository.GetUserRolesAsync(userId);
            if (userRoles.Any(r => r.Id == roleId))
                return BadRequest("El usuario ya tiene este rol");

            var userRole = new UserRole { UserId = userId, RoleId = roleId };
            await _context.UserRoles.AddAsync(userRole);
            user.SecurityVersion++;
            await _context.SaveChangesAsync();
            await _jwtService.RevokeAllRefreshTokensAsync(userId);

            return Ok(new { message = "Rol asignado exitosamente" });
        }

        private async Task<bool> CanAssignRolesAsync(IEnumerable<Role> roles)
        {
            var actorPermissions = User.FindAll(SecurityClaimTypes.Permission)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.Ordinal);
            var roleIds = roles.Select(role => role.Id).Distinct().ToList();
            var requiredPermissions = await _context.RolePermissions
                .Where(rolePermission => roleIds.Contains(rolePermission.RoleId) && !rolePermission.Permission.IsDeleted)
                .Select(rolePermission => rolePermission.Permission.Name)
                .Distinct()
                .ToListAsync();

            return requiredPermissions.All(actorPermissions.Contains);
        }
    }
}


