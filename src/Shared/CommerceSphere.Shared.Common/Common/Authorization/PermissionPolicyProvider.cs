using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CommerceSphere.Shared.Common.Authorization;

// Builds a policy on the fly for any "[HasPermission(...)]" usage (policy name "perm:{permission}")
// so we don't have to pre-register a named policy per permission. Anything else (e.g. "AdminOnly",
// the default [Authorize] policy) is delegated to the framework's default provider untouched.
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public const string Prefix = "perm:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) =>
        _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName[Prefix.Length..]))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
