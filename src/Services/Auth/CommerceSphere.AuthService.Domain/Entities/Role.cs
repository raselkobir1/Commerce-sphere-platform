namespace CommerceSphere.AuthService.Domain.Entities;

// A role groups a set of menu permissions. The User.Role string equals a Role.Name, so the JWT
// role claim and existing [Authorize(Roles=...)] checks keep working unchanged.
public class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }   // built-in roles (Admin/Customer) — cannot be deleted
    public bool IsDefault { get; private set; }   // assigned to newly self-registered users

    public ICollection<RoleMenuPermission> Permissions { get; private set; } = [];

    private Role() { }

    public static Role Create(string name, string? description, bool isSystem = false, bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Role
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            IsSystem = isSystem,
            IsDefault = isDefault,
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        // System role names are fixed (the JWT/role checks depend on them); only the description changes.
        if (!IsSystem) Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SetUpdated();
    }
}
