namespace CommerceSphere.AuthService.Domain.Entities;

// Grants a role a set of CRUD capabilities on a single menu. One row per (role, menu).
public class RoleMenuPermission : BaseEntity
{
    public Guid RoleId { get; private set; }
    public Guid MenuId { get; private set; }
    public bool CanView { get; private set; }
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanDelete { get; private set; }

    public Menu Menu { get; private set; } = null!;

    private RoleMenuPermission() { }

    public static RoleMenuPermission Create(Guid roleId, Guid menuId, bool canView, bool canCreate, bool canEdit, bool canDelete) =>
        new()
        {
            RoleId = roleId,
            MenuId = menuId,
            CanView = canView,
            CanCreate = canCreate,
            CanEdit = canEdit,
            CanDelete = canDelete,
        };

    public void Set(bool canView, bool canCreate, bool canEdit, bool canDelete)
    {
        CanView = canView;
        CanCreate = canCreate;
        CanEdit = canEdit;
        CanDelete = canDelete;
        SetUpdated();
    }
}
