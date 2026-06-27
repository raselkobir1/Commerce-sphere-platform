namespace CommerceSphere.AuthService.Application.DTOs.Requests;

public record CreateRoleRequest(string Name, string? Description);
public record UpdateRoleRequest(string Name, string? Description);

public record CreateMenuRequest(string Key, string Label, string Route, string Icon, int SortOrder, Guid? ParentId);
public record UpdateMenuRequest(string Label, string Route, string Icon, int SortOrder, Guid? ParentId);

public record MenuPermissionItem(Guid MenuId, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);
public record SetPermissionsRequest(IReadOnlyList<MenuPermissionItem> Permissions);

// Admin user management (distinct from public self-registration).
public record AdminCreateUserRequest(string Email, string Password, string FirstName, string LastName, string Role);
public record AdminUpdateUserRequest(string Role, bool IsActive);
