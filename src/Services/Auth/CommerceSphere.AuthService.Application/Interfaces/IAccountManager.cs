using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IAccountManager
{
    Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<AuthTokenResponse> CompleteForcedPasswordChangeAsync(ForcedPasswordChangeRequest request, string ipAddress, CancellationToken ct = default);

    // Email verification
    Task SendVerificationEmailAsync(Guid userId, CancellationToken ct = default);
    Task ResendVerificationEmailAsync(ResendVerificationEmailRequest request, CancellationToken ct = default);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);

    // Sessions
    Task<IReadOnlyList<SessionResponse>> GetActiveSessionsAsync(Guid userId, CancellationToken ct = default);
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default);
}
