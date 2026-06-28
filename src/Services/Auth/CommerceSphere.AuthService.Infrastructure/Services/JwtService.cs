using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.Shared.Common.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CommerceSphere.AuthService.Infrastructure.Services;

public class JwtService(IConfiguration config) : IJwtService
{
    private readonly string _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "CommerceSphere";
    private readonly string _audience = config["Jwt:Audience"] ?? "CommerceSphereClients";
    // GetValue<int> is safe when the key is missing or blank; int.Parse would throw on empty string.
    private readonly int _expiryMinutes = config.GetValue<int>("Jwt:ExpiryMinutes", 60);

    public string GenerateAccessToken(User user, IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            // Jti (JWT ID) is a unique identifier per token; downstream services can use it
            // to detect token replay if a revocation list is ever added.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Iat (issued-at) lets consumers detect tokens issued before a password-reset event.
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Granular RBAC grants ("products:create", …) — consumed by [HasPermission(...)].
        foreach (var permission in permissions)
            claims.Add(new Claim(PermissionClaims.Type, permission));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: GetAccessTokenExpiry(),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetAccessTokenExpiry() => DateTime.UtcNow.AddMinutes(_expiryMinutes);
}
