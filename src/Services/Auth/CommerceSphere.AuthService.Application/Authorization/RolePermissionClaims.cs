using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.Shared.Common.Authorization;

namespace CommerceSphere.AuthService.Application.Authorization;

// Flattens a role's menu permission rows into the "{menuKey}:{action}" claim strings embedded in
// the access token. One row (a menu) can yield up to four claims (view/create/edit/delete).
public static class RolePermissionClaims
{
    public static IReadOnlyList<string> Build(IEnumerable<RoleMenuPermission> permissions)
    {
        var claims = new List<string>();
        foreach (var p in permissions)
        {
            if (p.Menu is null) continue;
            var key = p.Menu.Key;
            if (p.CanView) claims.Add(PermissionClaims.For(key, PermissionClaims.ActionView));
            if (p.CanCreate) claims.Add(PermissionClaims.For(key, PermissionClaims.ActionCreate));
            if (p.CanEdit) claims.Add(PermissionClaims.For(key, PermissionClaims.ActionEdit));
            if (p.CanDelete) claims.Add(PermissionClaims.For(key, PermissionClaims.ActionDelete));
        }
        return claims;
    }
}
