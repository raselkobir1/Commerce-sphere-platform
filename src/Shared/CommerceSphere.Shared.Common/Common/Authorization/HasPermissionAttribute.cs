using Microsoft.AspNetCore.Authorization;

namespace CommerceSphere.Shared.Common.Authorization;

// Endpoint guard for a granular RBAC permission, e.g. [HasPermission("products:create")].
// Resolved by PermissionPolicyProvider + PermissionAuthorizationHandler. Admins always pass.
public sealed class HasPermissionAttribute(string permission)
    : AuthorizeAttribute(PermissionPolicyProvider.Prefix + permission);
