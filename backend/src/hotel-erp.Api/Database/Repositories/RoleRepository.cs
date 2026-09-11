using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context) => _context = context;

        public async Task<Role?> GetByIdAsync(Guid id)
            => await _context.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        public async Task<Role?> GetByNameAsync(string name)
        {
            var normalizedName = SecurityCatalogSeeder.NormalizeRoleName(name);
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.NormalizedName == normalizedName);
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
            => await _context.Roles.Where(r => !r.IsDeleted).Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).ToListAsync();

        public async Task AddAsync(Role role) { await _context.Roles.AddAsync(role); await _context.SaveChangesAsync(); }

        public async Task UpdateAsync(Role role) { _context.Roles.Update(role); await _context.SaveChangesAsync(); }

        public async Task DeleteAsync(Guid id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null) { role.IsDeleted = true; await _context.SaveChangesAsync(); }
        }

        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId)
            => await _context.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.Permission).ToListAsync();

        public async Task AssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            var existing = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            _context.RolePermissions.RemoveRange(existing);

            foreach (var permId in permissionIds)
            {
                await _context.RolePermissions.AddAsync(new RolePermission { RoleId = roleId, PermissionId = permId });
            }
            await _context.SaveChangesAsync();
        }
    }

    public class PermissionRepository : IPermissionRepository
    {
        private readonly ApplicationDbContext _context;

        public PermissionRepository(ApplicationDbContext context) => _context = context;

        public async Task<Permission?> GetByIdAsync(Guid id) => await _context.Permissions.FindAsync(id);
        public async Task<Permission?> GetByNameAsync(string name) => await _context.Permissions.FirstOrDefaultAsync(p => p.Name == name);
        public async Task<IEnumerable<Permission>> GetAllAsync() => await _context.Permissions.ToListAsync();
        public async Task AddAsync(Permission permission) { await _context.Permissions.AddAsync(permission); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var p = await _context.Permissions.FindAsync(id); if (p != null) { p.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }
}
