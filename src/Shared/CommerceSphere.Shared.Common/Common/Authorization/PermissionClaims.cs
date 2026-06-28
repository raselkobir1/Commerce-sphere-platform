namespace CommerceSphere.Shared.Common.Authorization;

// Granular RBAC permissions travel in the JWT as repeated claims of this type, each valued
// "{menuKey}:{action}" — e.g. "products:create", "inventory:view". The Auth service mints them
// at login from the user's role→menu permission matrix; every service enforces them.
public static class PermissionClaims
{
    public const string Type = "perm";

    public const string ActionView = "view";
    public const string ActionCreate = "create";
    public const string ActionEdit = "edit";
    public const string ActionDelete = "delete";

    public static string For(string menuKey, string action) => $"{menuKey}:{action}";
}
