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
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IMapper _mapper;

        public RolesController(IRoleRepository roleRepository, IPermissionRepository permissionRepository, IMapper mapper)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
        {
            var roles = await _roleRepository.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<RoleDto>>(roles));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetById(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return NotFound();
            return Ok(_mapper.Map<RoleDto>(role));
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request)
        {
            var existing = await _roleRepository.GetByNameAsync(request.Name);
            if (existing != null) return BadRequest("El rol ya existe");

            var role = new Domain.Entities.Role
            {
                Name = request.Name,
                Description = request.Description
            };
            await _roleRepository.AddAsync(role);

            if (request.Permissions?.Any() == true)
            {
                var permissions = await _permissionRepository.GetAllAsync();
                var selectedPermissions = permissions.Where(p => request.Permissions.Contains(p.Name)).ToList();
                foreach (var perm in selectedPermissions)
                {
                    // Add role permission - need DbContext for this
                }
            }

            return CreatedAtAction(nameof(GetById), new { id = role.Id }, _mapper.Map<RoleDto>(role));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return NotFound();

            if (request.Name != null) role.Name = request.Name;
            if (request.Description != null) role.Description = request.Description;
            await _roleRepository.UpdateAsync(role);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _roleRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions()
        {
            var permissions = await _permissionRepository.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<PermissionDto>>(permissions));
        }
    }
}
