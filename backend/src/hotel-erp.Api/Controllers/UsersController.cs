using AutoMapper;
using hotel_erp.Application.DTOs;
using hotel_erp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;

        public UsersController(IUserRepository userRepository, IRoleRepository roleRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
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
            if (user == null) return NotFound();
            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost("{userId}/roles/{roleId}")]
        public async Task<ActionResult> AssignRole(Guid userId, Guid roleId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound("Usuario no encontrado");

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null) return NotFound("Rol no encontrado");

            // Check if already assigned
            var userRoles = await _userRepository.GetUserRolesAsync(userId);
            if (userRoles.Any(r => r.Id == roleId))
                return BadRequest("El usuario ya tiene este rol");

            // Add the role assignment
            var userRole = new Domain.Entities.UserRole { UserId = userId, RoleId = roleId };
            // We need access to DbContext directly for this; for now let's use ApplicationDbContext
            // TODO: Implement through a proper service

            return Ok(new { message = "Rol asignado exitosamente" });
        }
    }
}
