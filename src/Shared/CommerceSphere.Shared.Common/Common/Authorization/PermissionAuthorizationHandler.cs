using Microsoft.AspNetCore.Authorization;

namespace CommerceSphere.Shared.Common.Authorization;

// Grants access when the caller either is an Admin (full-access safety net, so an admin can never
// be locked out of managing the system) or carries the exact "{menuKey}:{action}" permission claim.
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var granted = context.User.Claims.Any(c =>
            c.Type == PermissionClaims.Type &&
            string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (granted)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
