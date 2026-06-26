using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface ITwoFactorManager
{
    Task<TwoFactorSetupResponse> SetupAsync(Guid userId, CancellationToken ct = default);
    Task<AuthTokenResponse> ConfirmSetupAsync(Guid userId, ConfirmTwoFactorRequest request, string ipAddress, CancellationToken ct = default);
    Task DisableAsync(Guid userId, DisableTwoFactorRequest request, CancellationToken ct = default);
    Task<AuthTokenResponse> VerifyChallengeAsync(TwoFactorChallengeRequest request, string ipAddress, CancellationToken ct = default);
}
