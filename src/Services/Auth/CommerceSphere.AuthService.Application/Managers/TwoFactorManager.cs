using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Application.Managers;

public class TwoFactorManager(
    IUnitOfWork uow,
    ITotpService totpService,
    IChallengeTokenService challengeTokenService,
    AuthManager authManager,
    ILogger<TwoFactorManager> logger) : ITwoFactorManager
{
    public async Task<TwoFactorSetupResponse> SetupAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        var secret = totpService.GenerateSecret();
        user.SetTwoFactorSecret(secret);
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        var qrUri = totpService.GetQrCodeUri(secret, user.Email);

        // Break the base32 secret into 4-char groups so users can type it manually.
        var segments = Enumerable.Range(0, secret.Length / 4)
            .Select(i => secret.Substring(i * 4, 4))
            .ToArray();

        return new TwoFactorSetupResponse(secret, qrUri, segments);
    }

    public async Task<AuthTokenResponse> ConfirmSetupAsync(
        Guid userId, ConfirmTwoFactorRequest request, string ipAddress, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new BusinessException("Two-factor setup has not been initiated. Call /api/auth/2fa/setup first.");

        if (!totpService.ValidateCode(user.TwoFactorSecret, request.Code))
            throw new BusinessException("Invalid authenticator code.");

        user.ConfirmTwoFactor();
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Two-factor auth enabled. UserId: {UserId}", userId);

        // Issue fresh tokens so the client gets the updated 2FA status immediately.
        var loginResult = await authManager.CompleteLoginForChallengeAsync(userId, ipAddress, ct);
        return ((LoginSucceeded)loginResult).Tokens;
    }

    public async Task DisableAsync(Guid userId, DisableTwoFactorRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!user.IsActiveTwoFactor)
            throw new BusinessException("Two-factor authentication is not enabled.");

        if (!totpService.ValidateCode(user.TwoFactorSecret!, request.Code))
            throw new BusinessException("Invalid authenticator code. Provide your current TOTP code to disable 2FA.");

        user.DisableTwoFactor();
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Two-factor auth disabled. UserId: {UserId}", userId);
    }

    public async Task<AuthTokenResponse> VerifyChallengeAsync(
        TwoFactorChallengeRequest request, string ipAddress, CancellationToken ct = default)
    {
        var result = await challengeTokenService.ValidateAndConsumeAsync(request.ChallengeToken, ct)
            ?? throw new UnauthorizedException("Challenge token is invalid or has expired.");

        if (result.Type != ChallengeType.TwoFactor)
            throw new UnauthorizedException("Challenge token is not valid for two-factor authentication.");

        var user = await uow.Users.GetByIdAsync(result.UserId, ct)
            ?? throw new NotFoundException(nameof(User), result.UserId);

        if (!totpService.ValidateCode(user.TwoFactorSecret!, request.Code))
            throw new UnauthorizedException("Invalid authenticator code.");

        var loginResult = await authManager.CompleteLoginForChallengeAsync(result.UserId, ipAddress, ct);
        return ((LoginSucceeded)loginResult).Tokens;
    }
}
