using Microsoft.AspNetCore.Authorization;

namespace CommerceSphere.Shared.Common.Authorization;

// Requires the caller to hold a specific "{menuKey}:{action}" permission.
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
