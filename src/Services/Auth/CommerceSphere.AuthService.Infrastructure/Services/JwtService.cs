using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CommerceSphere.AuthService.Infrastructure.Services;

public class JwtService(IConfiguration config) : IJwtService
{
    private readonly string _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "CommerceSphere";
    private readonly string _audience = config["Jwt:Audience"] ?? "CommerceSphereClients";
    private readonly int _expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            // Jti (JWT ID) is a unique identifier per token; downstream services can use it
            // to detect token replay if a revocation list is ever added.
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Iat (issued-at) lets consumers detect tokens issued before a password-reset event.
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

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
