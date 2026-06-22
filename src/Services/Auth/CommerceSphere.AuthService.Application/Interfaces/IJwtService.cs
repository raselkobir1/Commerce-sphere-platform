using CommerceSphere.AuthService.Domain.Entities;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    DateTime GetAccessTokenExpiry();
}
