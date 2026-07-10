using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CommerceSphere.ChatService.Application.DTOs.Requests;

namespace CommerceSphere.ChatService.API.Auth;

// Builds the transport-agnostic ChatUser from the JWT ClaimsPrincipal. Tolerant of both the raw
// JWT claim names ("sub", "email") and the ASP.NET-mapped equivalents (NameIdentifier, Email),
// so it works whether or not inbound-claim mapping is enabled.
public static class ChatUserExtensions
{
    public static ChatUser ToChatUser(this ClaimsPrincipal principal)
    {
        var idValue =
            First(principal, ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub, "sub");
        if (!Guid.TryParse(idValue, out var userId))
            throw new InvalidOperationException("Authenticated user has no valid id claim.");

        var email = First(principal, ClaimTypes.Email, JwtRegisteredClaimNames.Email, "email") ?? string.Empty;
        var role  = First(principal, ClaimTypes.Role, "role") ?? string.Empty;

        var first = principal.FindFirst("firstName")?.Value ?? string.Empty;
        var last  = principal.FindFirst("lastName")?.Value ?? string.Empty;
        var name  = $"{first} {last}".Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = email.Split('@').FirstOrDefault() ?? "User";

        // Support agents are the Admin role in this platform.
        var isSupport = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        return new ChatUser(userId, name, email, isSupport);
    }

    private static string? First(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var type in claimTypes)
        {
            var value = principal.FindFirst(type)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
    }
}
