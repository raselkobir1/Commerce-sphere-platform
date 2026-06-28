using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceSphere.Shared.Common.Authorization;

public static class PermissionAuthorizationExtensions
{
    // Wires up granular permission checks ([HasPermission("...")]). Call after AddAuthorization();
    // the custom policy provider delegates non-permission policies to the default one, so existing
    // named/role policies keep working.
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
