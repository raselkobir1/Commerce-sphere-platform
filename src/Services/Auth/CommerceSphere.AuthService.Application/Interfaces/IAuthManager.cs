using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.Shared.Common.Models;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IAuthManager
{
    Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, string ipAddress, string correlationId, CancellationToken ct = default);
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, string ipAddress, string correlationId, CancellationToken ct = default);
    Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, CancellationToken ct = default);
    Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken ct = default);
    Task<PagedResult<UserResponse>> GetUsersAsync(PagedRequest paged, CancellationToken ct = default);
    Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken ct = default);
}
