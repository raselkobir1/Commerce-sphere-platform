using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Application.Managers;

public class RbacManager(IUnitOfWork uow, ILogger<RbacManager> logger) : IRbacManager
{
    // ── Roles ──────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken ct = default)
    {
        var roles = await uow.Roles.GetAllAsync(ct);
        return roles.Select(MapRole).ToList();
    }

    public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        if (await uow.Roles.ExistsByNameAsync(request.Name, null, ct))
            throw new ConflictException($"A role named '{request.Name.Trim()}' already exists.");

        var role = Role.Create(request.Name, request.Description);
        await uow.Roles.AddAsync(role, ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Role created. RoleId: {RoleId}, Name: {Name}", role.Id, role.Name);
        return MapRole(role);
    }

    public async Task<RoleResponse> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await uow.Roles.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Role), id);
        if (await uow.Roles.ExistsByNameAsync(request.Name, id, ct))
            throw new ConflictException($"A role named '{request.Name.Trim()}' already exists.");

        role.Update(request.Name, request.Description);
        uow.Roles.Update(role);
        await uow.SaveChangesAsync(ct);
        return MapRole(role);
    }

    public async Task DeleteRoleAsync(Guid id, CancellationToken ct = default)
    {
        var role = await uow.Roles.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Role), id);
        if (role.IsSystem)
            throw new BusinessException("System roles cannot be deleted.");

        var inUse = await uow.Users.CountByRoleAsync(role.Name, ct);
        if (inUse > 0)
            throw new BusinessException($"Cannot delete '{role.Name}' — {inUse} user(s) still have this role.");

        uow.Roles.Remove(role); // permissions cascade
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Role deleted. RoleId: {RoleId}", id);
    }

    // ── Menus ──────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<MenuResponse>> GetMenusAsync(CancellationToken ct = default)
    {
        var menus = await uow.Menus.GetAllAsync(ct);
        return menus.Select(MapMenu).ToList();
    }

    public async Task<MenuResponse> CreateMenuAsync(CreateMenuRequest request, CancellationToken ct = default)
    {
        if (await uow.Menus.ExistsByKeyAsync(request.Key, null, ct))
            throw new ConflictException($"A menu with key '{request.Key.Trim().ToLowerInvariant()}' already exists.");

        await ValidateParentAsync(request.ParentId, null, ct);

        var menu = Menu.Create(request.Key, request.Label, request.Route, request.Icon, request.SortOrder, request.ParentId);
        await uow.Menus.AddAsync(menu, ct);
        await uow.SaveChangesAsync(ct);

        // The Admin role always has full access to every menu, including newly created ones.
        var admin = await uow.Roles.GetByNameAsync("Admin", ct);
        if (admin is not null)
        {
            await uow.Permissions.AddAsync(RoleMenuPermission.Create(admin.Id, menu.Id, true, true, true, true), ct);
            await uow.SaveChangesAsync(ct);
        }

        return MapMenu(menu);
    }

    public async Task<MenuResponse> UpdateMenuAsync(Guid id, UpdateMenuRequest request, CancellationToken ct = default)
    {
        var menu = await uow.Menus.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Menu), id);
        await ValidateParentAsync(request.ParentId, id, ct);
        menu.Update(request.Label, request.Route, request.Icon, request.SortOrder, request.ParentId);
        uow.Menus.Update(menu);
        await uow.SaveChangesAsync(ct);
        return MapMenu(menu);
    }

    public async Task DeleteMenuAsync(Guid id, CancellationToken ct = default)
    {
        var menu = await uow.Menus.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Menu), id);

        var all = await uow.Menus.GetAllAsync(ct);
        if (all.Any(m => m.ParentId == id))
            throw new BusinessException("This menu has child menus. Delete or reparent them first.");

        uow.Menus.Remove(menu); // permissions referencing it cascade
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Menu deleted. MenuId: {MenuId}", id);
    }

    // Enforces a clean 2-level tree: a parent must exist, must itself be top-level, can't be the
    // menu itself, and a menu that already has children can't be turned into a child.
    private async Task ValidateParentAsync(Guid? parentId, Guid? selfId, CancellationToken ct)
    {
        if (parentId is null) return;
        if (parentId == selfId)
            throw new BusinessException("A menu cannot be its own parent.");

        var parent = await uow.Menus.GetByIdAsync(parentId.Value, ct)
            ?? throw new BusinessException("Selected parent menu does not exist.");
        if (parent.ParentId is not null)
            throw new BusinessException("Menus support two levels only — the parent must be a top-level menu.");

        if (selfId is not null)
        {
            var all = await uow.Menus.GetAllAsync(ct);
            if (all.Any(m => m.ParentId == selfId))
                throw new BusinessException("This menu has child menus, so it cannot become a child itself.");
        }
    }

    // ── Permissions ────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<MenuPermissionResponse>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default)
    {
        _ = await uow.Roles.GetByIdAsync(roleId, ct) ?? throw new NotFoundException(nameof(Role), roleId);

        var menus = await uow.Menus.GetAllAsync(ct);
        var existing = (await uow.Permissions.GetByRoleIdAsync(roleId, ct)).ToDictionary(p => p.MenuId);

        return menus.Select(m =>
        {
            existing.TryGetValue(m.Id, out var p);
            return new MenuPermissionResponse(m.Id, m.Key, m.Label, m.Route, m.Icon, m.SortOrder, m.ParentId,
                p?.CanView ?? false, p?.CanCreate ?? false, p?.CanEdit ?? false, p?.CanDelete ?? false);
        }).ToList();
    }

    public async Task SetRolePermissionsAsync(Guid roleId, SetPermissionsRequest request, CancellationToken ct = default)
    {
        _ = await uow.Roles.GetByIdAsync(roleId, ct) ?? throw new NotFoundException(nameof(Role), roleId);

        var existing = (await uow.Permissions.GetByRoleIdAsync(roleId, ct)).ToDictionary(p => p.MenuId);

        foreach (var item in request.Permissions)
        {
            if (existing.TryGetValue(item.MenuId, out var current))
            {
                current.Set(item.CanView, item.CanCreate, item.CanEdit, item.CanDelete);
            }
            else
            {
                // New entity added via AddAsync → tracked as Added (correct INSERT), avoiding the
                // "client-generated key marked Modified" pitfall.
                await uow.Permissions.AddAsync(
                    RoleMenuPermission.Create(roleId, item.MenuId, item.CanView, item.CanCreate, item.CanEdit, item.CanDelete), ct);
            }
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Permissions updated for RoleId: {RoleId} ({Count} menus)", roleId, request.Permissions.Count);
    }

    public async Task<IReadOnlyList<MenuPermissionResponse>> GetMenusForRoleAsync(string roleName, CancellationToken ct = default)
    {
        var perms = await uow.Permissions.GetByRoleNameAsync(roleName, ct);
        return perms
            .Where(p => p.CanView && p.Menu is not null)
            .OrderBy(p => p.Menu.SortOrder)
            .Select(p => new MenuPermissionResponse(p.MenuId, p.Menu.Key, p.Menu.Label, p.Menu.Route, p.Menu.Icon,
                p.Menu.SortOrder, p.Menu.ParentId, p.CanView, p.CanCreate, p.CanEdit, p.CanDelete))
            .ToList();
    }

    private static RoleResponse MapRole(Role r) => new(r.Id, r.Name, r.Description, r.IsSystem, r.IsDefault, r.CreatedAt);
    private static MenuResponse MapMenu(Menu m) => new(m.Id, m.Key, m.Label, m.Route, m.Icon, m.SortOrder, m.ParentId);
}
