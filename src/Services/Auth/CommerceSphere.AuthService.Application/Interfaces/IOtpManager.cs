using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IOtpManager
{
    Task<AuthTokenResponse> VerifyChallengeAsync(OtpChallengeRequest request, string ipAddress, CancellationToken ct = default);
    Task ToggleOtpAuthAsync(Guid userId, ToggleOtpRequest request, CancellationToken ct = default);
}
