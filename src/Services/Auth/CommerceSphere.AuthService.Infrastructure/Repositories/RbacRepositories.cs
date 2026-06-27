using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces.Repositories;
using CommerceSphere.AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.AuthService.Infrastructure.Repositories;

public class RoleRepository(AuthDbContext db) : IRoleRepository
{
    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default) =>
        await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return db.Roles.AnyAsync(r => r.Name.ToLower() == normalized && (excludeId == null || r.Id != excludeId), ct);
    }

    public async Task AddAsync(Role role, CancellationToken ct = default) => await db.Roles.AddAsync(role, ct);
    public void Update(Role role) => db.Roles.Update(role);
    public void Remove(Role role) => db.Roles.Remove(role);
}

public class MenuRepository(AuthDbContext db) : IMenuRepository
{
    public async Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken ct = default) =>
        await db.Menus.AsNoTracking().OrderBy(m => m.SortOrder).ThenBy(m => m.Label).ToListAsync(ct);

    public Task<Menu?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Menus.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<bool> ExistsByKeyAsync(string key, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = key.Trim().ToLower();
        return db.Menus.AnyAsync(m => m.Key == normalized && (excludeId == null || m.Id != excludeId), ct);
    }

    public async Task AddAsync(Menu menu, CancellationToken ct = default) => await db.Menus.AddAsync(menu, ct);
    public void Update(Menu menu) => db.Menus.Update(menu);
    public void Remove(Menu menu) => db.Menus.Remove(menu);
}

public class RoleMenuPermissionRepository(AuthDbContext db) : IRoleMenuPermissionRepository
{
    public Task<List<RoleMenuPermission>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default) =>
        db.RolePermissions.Where(p => p.RoleId == roleId).ToListAsync(ct);

    public async Task<IReadOnlyList<RoleMenuPermission>> GetByRoleNameAsync(string roleName, CancellationToken ct = default)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null) return [];
        return await db.RolePermissions.AsNoTracking()
            .Include(p => p.Menu)
            .Where(p => p.RoleId == role.Id)
            .ToListAsync(ct);
    }

    public async Task AddAsync(RoleMenuPermission permission, CancellationToken ct = default) =>
        await db.RolePermissions.AddAsync(permission, ct);
}
