using hotel_erp.Api.Dtos.Auth;
using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = PermissionNames.ManageRoles)]
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;

        public RolesController(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            ApplicationDbContext context,
            IJwtService jwtService,
            IMapper mapper)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _context = context;
            _jwtService = jwtService;
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

            var selectedPermissions = await ResolveAssignablePermissionsAsync(request.Permissions);
            if (selectedPermissions is null)
                return BadRequest("Uno o más permisos solicitados no existen o exceden los permisos del operador");

            var role = new Role
            {
                Name = request.Name.Trim(),
                NormalizedName = SecurityCatalogSeeder.NormalizeRoleName(request.Name),
                Description = request.Description
            };
            await _roleRepository.AddAsync(role);

            if (selectedPermissions.Count > 0)
            {
                await _roleRepository.AssignPermissionsAsync(role.Id, selectedPermissions.Select(permission => permission.Id));
            }

            return CreatedAtAction(nameof(GetById), new { id = role.Id }, _mapper.Map<RoleDto>(role));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return NotFound();

            if (request.Name != null)
            {
                var normalizedName = SecurityCatalogSeeder.NormalizeRoleName(request.Name);
                if (role.SystemKey is not null && normalizedName != role.NormalizedName)
                    return BadRequest("Los roles del sistema no se pueden renombrar");

                var duplicate = await _context.Roles.AnyAsync(candidate =>
                    candidate.Id != id && !candidate.IsDeleted && candidate.NormalizedName == normalizedName);
                if (duplicate)
                    return BadRequest("El rol ya existe");

                role.Name = request.Name.Trim();
                role.NormalizedName = normalizedName;
            }

            List<Permission>? selectedPermissions = null;
            if (request.Permissions is not null)
            {
                selectedPermissions = await ResolveAssignablePermissionsAsync(request.Permissions);
                if (selectedPermissions is null)
                    return BadRequest("Uno o más permisos solicitados no existen o exceden los permisos del operador");
            }

            if (request.Description != null) role.Description = request.Description;
            await _roleRepository.UpdateAsync(role);

            if (selectedPermissions is not null)
                await _roleRepository.AssignPermissionsAsync(role.Id, selectedPermissions.Select(permission => permission.Id));

            await InvalidateRoleSessionsAsync(role.Id);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return NotFound();
            if (role.SystemKey is not null)
                return BadRequest("Los roles del sistema no se pueden eliminar");

            await _roleRepository.DeleteAsync(id);
            await InvalidateRoleSessionsAsync(id);
            return NoContent();
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions()
        {
            var permissions = await _permissionRepository.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<PermissionDto>>(permissions));
        }

        private async Task<List<Permission>?> ResolveAssignablePermissionsAsync(IEnumerable<string>? permissionNames)
        {
            var requested = (permissionNames ?? [])
                .Select(name => name.Trim())
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (requested.Count == 0)
                return [];

            var available = (await _permissionRepository.GetAllAsync())
                .Where(permission => !permission.IsDeleted && requested.Contains(permission.Name, StringComparer.Ordinal))
                .ToList();
            if (available.Count != requested.Count)
                return null;

            var actorPermissions = User.FindAll(SecurityClaimTypes.Permission)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.Ordinal);
            return available.All(permission => actorPermissions.Contains(permission.Name)) ? available : null;
        }

        private async Task InvalidateRoleSessionsAsync(Guid roleId)
        {
            var users = await _context.UserRoles
                .Where(userRole => userRole.RoleId == roleId)
                .Select(userRole => userRole.User)
                .Distinct()
                .ToListAsync();

            foreach (var user in users)
                user.SecurityVersion++;

            await _context.SaveChangesAsync();
            foreach (var user in users)
                await _jwtService.RevokeAllRefreshTokensAsync(user.Id);
        }
    }
}


