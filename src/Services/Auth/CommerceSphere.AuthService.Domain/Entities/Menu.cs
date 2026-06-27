namespace CommerceSphere.AuthService.Domain.Entities;

// A navigable admin menu item. Roles are granted CRUD permissions per menu (RoleMenuPermission).
public class Menu : BaseEntity
{
    public string Key { get; private set; } = string.Empty;   // stable id, e.g. "products"
    public string Label { get; private set; } = string.Empty; // sidebar text
    public string Route { get; private set; } = string.Empty; // frontend route, e.g. "/products"
    public string Icon { get; private set; } = string.Empty;  // emoji icon
    public int SortOrder { get; private set; }
    public Guid? ParentId { get; private set; }               // null = top-level; set = child menu

    private Menu() { }

    public static Menu Create(string key, string label, string route, string icon, int sortOrder, Guid? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new Menu
        {
            Key = key.Trim().ToLowerInvariant(),
            Label = label.Trim(),
            Route = route.Trim(),
            Icon = icon?.Trim() ?? string.Empty,
            SortOrder = sortOrder,
            ParentId = parentId,
        };
    }

    public void Update(string label, string route, string icon, int sortOrder, Guid? parentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label.Trim();
        Route = route.Trim();
        Icon = icon?.Trim() ?? string.Empty;
        SortOrder = sortOrder;
        ParentId = parentId;
        SetUpdated();
    }
}
