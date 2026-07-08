using BC = BCrypt.Net.BCrypt;
using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Application.Managers;

public class AccountManager(
    IUnitOfWork uow,
    IEmailService emailService,
    IChallengeTokenService challengeTokenService,
    AuthManager authManager,
    ILogger<AccountManager> logger) : IAccountManager
{
    public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        user.UpdateProfile(request.FirstName, request.LastName);
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        return AuthManager.MapToResponse(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (string.IsNullOrEmpty(user.PasswordHash) || !BC.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        user.ChangePassword(BC.HashPassword(request.NewPassword));
        uow.Users.Update(user);

        // Revoke all existing refresh tokens so other sessions are invalidated.
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
            uow.RefreshTokens.Update(token);
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Password changed. UserId: {UserId}", userId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByEmailAsync(request.Email, ct);

        // Always return success even when the email isn't found — prevents user enumeration.
        if (user is null) return;

        var token = user.GeneratePasswordResetToken();
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        _ = emailService.SendPasswordResetAsync(user.Email, user.FirstName, token, user.Role == "Admin", ct)
            .ContinueWith(t => logger.LogWarning(t.Exception, "Failed to send password reset email to {Email}", user.Email),
                TaskContinuationOptions.OnlyOnFaulted);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByPasswordResetTokenAsync(request.Token, ct)
            ?? throw new BusinessException("Password reset token is invalid or has expired.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new BusinessException("Password reset token has expired. Please request a new one.");

        user.ChangePassword(BC.HashPassword(request.NewPassword));
        user.ClearPasswordResetToken();

        // Revoke all sessions — user may have lost access to their account.
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
            uow.RefreshTokens.Update(token);
        }

        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Password reset completed. UserId: {UserId}", user.Id);
    }

    public async Task<AuthTokenResponse> CompleteForcedPasswordChangeAsync(
        ForcedPasswordChangeRequest request, string ipAddress, CancellationToken ct = default)
    {
        var result = await challengeTokenService.ValidateAndConsumeAsync(request.ChallengeToken, ct)
            ?? throw new UnauthorizedException("Challenge token is invalid or has expired.");

        if (result.Type != ChallengeType.PasswordChange)
            throw new UnauthorizedException("Challenge token is not valid for a password change.");

        var user = await uow.Users.GetByIdAsync(result.UserId, ct)
            ?? throw new NotFoundException(nameof(User), result.UserId);

        user.ChangePassword(BC.HashPassword(request.NewPassword));
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Forced password change completed. UserId: {UserId}", user.Id);

        var loginResult = await authManager.CompleteLoginForChallengeAsync(user.Id, ipAddress, ct);
        return ((LoginSucceeded)loginResult).Tokens;
    }

    public async Task SendVerificationEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (user.EmailVerified)
            throw new BusinessException("Email is already verified.");

        var token = user.GenerateEmailVerificationToken();
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        await emailService.SendEmailVerificationAsync(user.Email, user.FirstName, token, ct);
    }

    public async Task ResendVerificationEmailAsync(ResendVerificationEmailRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByEmailAsync(request.Email, ct);

        // Silent return when email not found — prevents enumeration.
        if (user is null || user.EmailVerified) return;

        var token = user.GenerateEmailVerificationToken();
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        await emailService.SendEmailVerificationAsync(user.Email, user.FirstName, token, ct);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByEmailVerificationTokenAsync(request.Token, ct)
            ?? throw new BusinessException("Verification token is invalid or has expired.");

        if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            throw new BusinessException("Verification token has expired. Please request a new one.");

        user.MarkEmailVerified();
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Email verified. UserId: {UserId}", user.Id);
    }

    public async Task<IReadOnlyList<SessionResponse>> GetActiveSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        return user.RefreshTokens
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SessionResponse(t.Id, t.CreatedByIp, t.CreatedAt, t.ExpiresAt, t.IsActive))
            .ToList();
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
            uow.RefreshTokens.Update(token);
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("All sessions revoked. UserId: {UserId}", userId);
    }
}
