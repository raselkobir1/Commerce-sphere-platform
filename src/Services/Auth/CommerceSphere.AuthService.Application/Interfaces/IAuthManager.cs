using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.Shared.Common.Models;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IAuthManager
{
    Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, string ipAddress, string correlationId, CancellationToken ct = default);
    Task<LoginResult> LoginAsync(LoginRequest request, string ipAddress, string correlationId, CancellationToken ct = default);
    Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, CancellationToken ct = default);
    Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken ct = default);
    Task<PagedResult<UserResponse>> GetUsersAsync(PagedRequest paged, CancellationToken ct = default);
    Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken ct = default);

    // Admin user management
    Task<UserResponse> AdminCreateUserAsync(AdminCreateUserRequest request, string correlationId, CancellationToken ct = default);
    Task<UserResponse> AdminUpdateUserAsync(Guid id, AdminUpdateUserRequest request, CancellationToken ct = default);
    Task AdminDeleteUserAsync(Guid id, CancellationToken ct = default);
}
