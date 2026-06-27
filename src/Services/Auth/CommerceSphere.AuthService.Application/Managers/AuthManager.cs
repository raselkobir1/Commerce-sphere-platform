using BC = BCrypt.Net.BCrypt;
using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Models;
using CommerceSphere.Shared.Contracts.Events.Auth;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Application.Managers;

public class AuthManager(
    IUnitOfWork uow,
    IJwtService jwtService,
    IUserEventProducer eventProducer,
    IEmailService emailService,
    IChallengeTokenService challengeTokenService,
    IOtpCodeService otpCodeService,
    ILogger<AuthManager> logger) : IAuthManager
{
    public async Task<AuthTokenResponse> RegisterAsync(
        RegisterRequest request, string ipAddress, string correlationId, CancellationToken ct = default)
    {
        if (await uow.Users.ExistsByEmailAsync(request.Email, ct))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var passwordHash = BC.HashPassword(request.Password);
        // SECURITY: never trust a client-supplied role on public self-registration — that would let
        // anyone create an Admin account. Public registration always creates a "Customer"; elevated
        // roles must be provisioned out-of-band (admin tooling / seed), not via this endpoint.
        var user = User.Create(request.Email, passwordHash, request.FirstName, request.LastName, role: "Customer");

        // Generate email verification token before first save so the token is durable.
        var verificationToken = user.GenerateEmailVerificationToken();

        await uow.Users.AddAsync(user, ct);

        var refreshToken = RefreshToken.Create(user.Id, ipAddress);
        await uow.RefreshTokens.AddAsync(refreshToken, ct);

        await uow.SaveChangesAsync(ct);

        logger.LogInformation("User registered. UserId: {UserId}, CorrelationId: {CorrelationId}", user.Id, correlationId);

        await eventProducer.PublishUserCreatedAsync(
            new UserCreatedEvent(user.Id, user.Email, user.FirstName, user.LastName,
                                 user.Role, DateTime.UtcNow, correlationId), ct);

        // Fire-and-forget: verification email failure should not break registration.
        _ = emailService.SendEmailVerificationAsync(user.Email, user.FirstName, verificationToken, ct)
            .ContinueWith(t => logger.LogWarning(t.Exception, "Failed to send verification email to {Email}", user.Email),
                TaskContinuationOptions.OnlyOnFaulted);

        return BuildTokenResponse(user, refreshToken);
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request, string ipAddress, string correlationId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (user.IsLockedOut())
            throw new UnauthorizedException("Account is temporarily locked due to too many failed attempts. Try again later.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!BC.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            uow.Users.Update(user);
            await uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        logger.LogInformation("User authenticated. UserId: {UserId}, CorrelationId: {CorrelationId}", user.Id, correlationId);

        // 2FA takes priority over OTP when both are enabled.
        if (user.IsActiveTwoFactor && user.TwoFactorConfirmed)
        {
            var challengeToken = await challengeTokenService.CreateAsync(user.Id, ChallengeType.TwoFactor, ct);
            return new LoginNeedsTwoFactor(challengeToken);
        }

        if (user.IsOtpAuthEnable)
        {
            var otp = await otpCodeService.GenerateAndStoreAsync(user.Id, ct);
            _ = emailService.SendOtpAsync(user.Email, user.FirstName, otp, ct)
                .ContinueWith(t => logger.LogWarning(t.Exception, "Failed to send OTP to {Email}", user.Email),
                    TaskContinuationOptions.OnlyOnFaulted);

            var challengeToken = await challengeTokenService.CreateAsync(user.Id, ChallengeType.Otp, ct);
            return new LoginNeedsOtp(challengeToken);
        }

        return await CompleteLoginAsync(user, ipAddress, ct);
    }

    // Called after a successful 2FA or OTP challenge — issues tokens.
    public async Task<LoginResult> CompleteLoginForChallengeAsync(
        Guid userId, string ipAddress, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        return await CompleteLoginAsync(user, ipAddress, ct);
    }

    public async Task<AuthTokenResponse> RefreshTokenAsync(
        RefreshTokenRequest request, string ipAddress, CancellationToken ct = default)
    {
        var existingToken = await uow.RefreshTokens.GetByTokenAsync(request.RefreshToken, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!existingToken.IsActive)
            throw new UnauthorizedException("Refresh token is expired or revoked.");

        var user = await uow.Users.GetByIdAsync(existingToken.UserId, ct)
            ?? throw new NotFoundException(nameof(User), existingToken.UserId);

        // SECURITY: a still-valid refresh token must not keep minting access tokens for an account
        // that has since been deactivated. Revoke the presented token and refuse.
        if (!user.IsActive)
        {
            existingToken.Revoke();
            uow.RefreshTokens.Update(existingToken);
            await uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("Account is deactivated.");
        }

        var newRefreshToken = RefreshToken.Create(user.Id, ipAddress);
        existingToken.Revoke(newRefreshToken.Token);

        uow.RefreshTokens.Update(existingToken);
        await uow.RefreshTokens.AddAsync(newRefreshToken, ct);
        await uow.SaveChangesAsync(ct);

        return BuildTokenResponse(user, newRefreshToken);
    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken ct = default)
    {
        var token = await uow.RefreshTokens.GetByTokenAsync(request.RefreshToken, ct)
            ?? throw new NotFoundException(nameof(RefreshToken), request.RefreshToken);

        if (!token.IsActive)
            throw new BusinessException("Token is already revoked or expired.");

        token.Revoke();
        uow.RefreshTokens.Update(token);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(PagedRequest paged, CancellationToken ct = default)
    {
        var (users, total) = await uow.Users.GetPagedAsync(paged.PageNumber, paged.PageSize, ct);
        return PagedResult<UserResponse>.Create(users.Select(MapToResponse), total, paged.PageNumber, paged.PageSize);
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        return MapToResponse(user);
    }

    private async Task<LoginResult> CompleteLoginAsync(User user, string ipAddress, CancellationToken ct)
    {
        user.RecordLogin();
        uow.Users.Update(user);

        var refreshToken = RefreshToken.Create(user.Id, ipAddress);
        await uow.RefreshTokens.AddAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        return new LoginSucceeded(BuildTokenResponse(user, refreshToken));
    }

    internal AuthTokenResponse BuildTokenResponse(User user, RefreshToken refreshToken) =>
        new(
            AccessToken: jwtService.GenerateAccessToken(user),
            RefreshToken: refreshToken.Token,
            ExpiresAt: jwtService.GetAccessTokenExpiry(),
            User: MapToResponse(user)
        );

    internal static UserResponse MapToResponse(User u) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive,
            u.EmailVerified, u.IsActiveTwoFactor, u.TwoFactorConfirmed, u.IsOtpAuthEnable,
            u.CreatedAt, u.LastLoginAt);
}
