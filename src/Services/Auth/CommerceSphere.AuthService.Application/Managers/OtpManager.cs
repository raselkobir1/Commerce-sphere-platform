using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Application.Managers;

public class OtpManager(
    IUnitOfWork uow,
    IOtpCodeService otpCodeService,
    IChallengeTokenService challengeTokenService,
    AuthManager authManager,
    ILogger<OtpManager> logger) : IOtpManager
{
    public async Task<AuthTokenResponse> VerifyChallengeAsync(
        OtpChallengeRequest request, string ipAddress, CancellationToken ct = default)
    {
        var result = await challengeTokenService.ValidateAndConsumeAsync(request.ChallengeToken, ct)
            ?? throw new UnauthorizedException("Challenge token is invalid or has expired.");

        if (result.Type != ChallengeType.Otp)
            throw new UnauthorizedException("Challenge token is not valid for OTP authentication.");

        var valid = await otpCodeService.ValidateAndConsumeAsync(result.UserId, request.Code, ct);
        if (!valid)
            throw new UnauthorizedException("Invalid or expired OTP code.");

        var loginResult = await authManager.CompleteLoginForChallengeAsync(result.UserId, ipAddress, ct);
        return ((LoginSucceeded)loginResult).Tokens;
    }

    public async Task ToggleOtpAuthAsync(Guid userId, ToggleOtpRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (request.Enable)
            user.EnableOtpAuth();
        else
            user.DisableOtpAuth();

        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("OTP auth {Status}. UserId: {UserId}", request.Enable ? "enabled" : "disabled", userId);
    }
}
