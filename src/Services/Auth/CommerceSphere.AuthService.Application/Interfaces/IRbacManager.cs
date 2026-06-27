using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IRbacManager
{
    // Roles
    Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleResponse> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default);
    Task DeleteRoleAsync(Guid id, CancellationToken ct = default);

    // Menus
    Task<IReadOnlyList<MenuResponse>> GetMenusAsync(CancellationToken ct = default);
    Task<MenuResponse> CreateMenuAsync(CreateMenuRequest request, CancellationToken ct = default);
    Task<MenuResponse> UpdateMenuAsync(Guid id, UpdateMenuRequest request, CancellationToken ct = default);
    Task DeleteMenuAsync(Guid id, CancellationToken ct = default);

    // Role ↔ menu permissions
    Task<IReadOnlyList<MenuPermissionResponse>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);
    Task SetRolePermissionsAsync(Guid roleId, SetPermissionsRequest request, CancellationToken ct = default);

    // The signed-in user's accessible menus (CanView only), for the dynamic sidebar.
    Task<IReadOnlyList<MenuPermissionResponse>> GetMenusForRoleAsync(string roleName, CancellationToken ct = default);
}
