using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Middleware;
using Microsoft.AspNetCore.Builder;

namespace CommerceSphere.Shared.Common.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
}
