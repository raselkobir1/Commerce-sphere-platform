using CommerceSphere.AuthService.Domain.Entities;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IJwtService
{
    // Permissions are the caller's "{menuKey}:{action}" grants, embedded as claims so every
    // service can enforce the RBAC matrix from the token alone.
    string GenerateAccessToken(User user, IEnumerable<string> permissions);
    DateTime GetAccessTokenExpiry();
}
