namespace CommerceSphere.AuthService.Application.DTOs.Responses;

public record RoleResponse(Guid Id, string Name, string Description, bool IsSystem, bool IsDefault, DateTime CreatedAt);

public record MenuResponse(Guid Id, string Key, string Label, string Route, string Icon, int SortOrder, Guid? ParentId);

// One menu + a role's CRUD flags for it. Used for the permission matrix (all menus) and for the
// signed-in user's own menus (only those with CanView).
public record MenuPermissionResponse(
    Guid MenuId,
    string MenuKey,
    string Label,
    string Route,
    string Icon,
    int SortOrder,
    Guid? ParentId,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete
);
