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

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
                return BadRequest(new { message = "El nombre de usuario ya está en uso" });

            existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new { message = "El correo electrónico ya está registrado" });

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true
            };

            await _userRepository.AddAsync(user);

            // Assign roles if provided, otherwise default to "Recepcion"
            var roleNames = request.Roles != null && request.Roles.Any()
                ? request.Roles
                : new List<string> { "Recepcion" };

            var allRoles = await _context.Roles.ToListAsync();
            foreach (var roleName in roleNames)
            {
                var role = allRoles.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));
                if (role != null)
                {
                    await _context.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
            }

            await _context.SaveChangesAsync();

            var createdUser = await _userRepository.GetByIdAsync(user.Id);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, _mapper.Map<UserDto>(createdUser));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound("Usuario no encontrado");

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
                // Remove existing user roles
                _context.UserRoles.RemoveRange(user.UserRoles);

                var allRoles = await _context.Roles.ToListAsync();
                foreach (var roleName in request.Roles)
                {
                    var role = allRoles.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));
                    if (role != null)
                    {
                        await _context.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id });
                    }
                }
            }

            await _context.SaveChangesAsync();
            await _jwtService.RevokeAllRefreshTokensAsync(id);

            var updatedUser = await _userRepository.GetByIdAsync(id);
            return Ok(_mapper.Map<UserDto>(updatedUser));
        }

        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound("Usuario no encontrado");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await _userRepository.UpdateAsync(user);
            await _jwtService.RevokeAllRefreshTokensAsync(id);

            return Ok(new { message = "Contraseña restablecida exitosamente" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != null && Guid.Parse(currentUserId) == id)
            {
                return BadRequest(new { message = "No puede eliminar o desactivar su propia cuenta de administrador" });
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound("Usuario no encontrado");

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);
            await _jwtService.RevokeAllRefreshTokensAsync(id);

            return Ok(new { message = "Usuario desactivado exitosamente" });
        }

        [HttpPost("{userId}/roles/{roleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignRole(Guid userId, Guid roleId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound("Usuario no encontrado");

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null) return NotFound("Rol no encontrado");

            var userRoles = await _userRepository.GetUserRolesAsync(userId);
            if (userRoles.Any(r => r.Id == roleId))
                return BadRequest("El usuario ya tiene este rol");

            var userRole = new UserRole { UserId = userId, RoleId = roleId };
            await _context.UserRoles.AddAsync(userRole);
            await _context.SaveChangesAsync();
            await _jwtService.RevokeAllRefreshTokensAsync(userId);

            return Ok(new { message = "Rol asignado exitosamente" });
        }
    }
}




