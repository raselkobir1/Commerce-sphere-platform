using CommerceSphere.AuthService.Application.Authorization;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Contracts.Events.Auth;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Application.Managers;

// Orchestrates the SSO login flow:
//   1. GetLoginUrlAsync   → delegates to SsoService to build the provider's OAuth auth URL
//   2. HandleCallbackAsync → exchanges the provider code for user info, then creates/links
//      a local account and issues our own JWT — same token format as password login
public class SsoManager(
    IUnitOfWork uow,
    IJwtService jwtService,
    ISsoService ssoService,
    IUserEventProducer eventProducer,
    ILogger<SsoManager> logger) : ISsoManager
{
    public Task<SsoLoginUrlResponse> GetLoginUrlAsync(
        string provider, string redirectUri, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new BusinessException("RedirectUri is required for SSO login.");

        // SsoService validates the provider, generates a random state token, stores
        // { provider, redirectUri } in Redis, and returns the full OAuth authorization URL.
        return ssoService.BuildLoginUrlAsync(provider, redirectUri, ct);
    }

    public async Task<SsoCallbackResult> HandleCallbackAsync(
        string code, string state, string ipAddress, string correlationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessException("Authorization code is missing from the SSO callback.");

        if (string.IsNullOrWhiteSpace(state))
            throw new BusinessException("State parameter is missing — possible CSRF attempt.");

        // Exchange the authorization code with the provider and retrieve the verified user identity.
        // SsoService also validates the state token against Redis to prevent CSRF.
        var (userInfo, provider, redirectUri) = await ssoService.ProcessCallbackAsync(code, state, ct);

        logger.LogInformation(
            "SSO callback received. Provider: {Provider}, Email: {Email}, Sub: {Sub}, CorrelationId: {CorrelationId}",
            provider, userInfo.Email, userInfo.Sub, correlationId);

        // --- Resolve or create local user ---
        // Wrapped in a DB transaction to prevent a race condition where two concurrent first-time
        // logins with the same email both see null and try to INSERT the same user row, causing
        // a unique constraint violation on ix_users_email. The catch block handles that case.

        User? user;
        bool isNewUser = false;

        await uow.BeginTransactionAsync(ct);
        try
        {
            // Step 1: look up an existing link by social identity (most common path for returning users).
            user = await uow.Users.GetByExternalLoginAsync(provider, userInfo.Sub, ct);

            if (user is null)
            {
                // Step 2: try to find a locally-registered account with the same email.
                // This handles the case where a user registered with email/password first and then
                // tries to log in with the same email via Google — we link the accounts automatically.
                user = await uow.Users.GetByEmailAsync(userInfo.Email, ct);

                if (user is null)
                {
                    // Step 3: first-ever login with this social identity — create a new local user.
                    user = User.CreateFromSso(userInfo.Email, userInfo.FirstName, userInfo.LastName);
                    await uow.Users.AddAsync(user, ct);

                    // Flush to DB now so user.Id is assigned before we create the ExternalLogin FK.
                    await uow.SaveChangesAsync(ct);
                    isNewUser = true;

                    logger.LogInformation(
                        "New SSO user created. UserId: {UserId}, Provider: {Provider}, CorrelationId: {CorrelationId}",
                        user.Id, provider, correlationId);
                }
                else
                {
                    logger.LogInformation(
                        "Existing local user linked to SSO. UserId: {UserId}, Provider: {Provider}, CorrelationId: {CorrelationId}",
                        user.Id, provider, correlationId);
                }

                // Create the ExternalLogin row so future logins via the same social account
                // are resolved directly (Step 1 path) without checking by email again.
                var externalLogin = ExternalLogin.Create(user.Id, provider, userInfo.Sub);
                await uow.Users.AddExternalLoginAsync(externalLogin, ct);
            }

            await uow.CommitTransactionAsync(ct);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            // Two concurrent first-time logins for the same email raced — the other request won.
            // Roll back, then fall back to the existing user.
            await uow.RollbackTransactionAsync(ct);
            user = await uow.Users.GetByEmailAsync(userInfo.Email, ct)
                ?? throw new SsoException("SSO login failed due to a concurrent signup conflict. Please try again.");
            logger.LogWarning(
                "Concurrent SSO signup conflict resolved. Email: {Email}, Provider: {Provider}", userInfo.Email, provider);
        }
        catch
        {
            await uow.RollbackTransactionAsync(ct);
            throw;
        }

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        // Record last login timestamp (same as password login).
        user.RecordLogin();
        uow.Users.Update(user);

        var refreshToken = RefreshToken.Create(user.Id, ipAddress);
        await uow.RefreshTokens.AddAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "SSO login complete. UserId: {UserId}, Provider: {Provider}, CorrelationId: {CorrelationId}",
            user.Id, provider, correlationId);

        // Publish after the transaction commits so the event is only sent when the user row is durable.
        if (isNewUser)
            await eventProducer.PublishUserCreatedAsync(
                new UserCreatedEvent(user.Id, user.Email, user.FirstName, user.LastName,
                                     user.Role, DateTime.UtcNow, correlationId), ct);

        var permissions = RolePermissionClaims.Build(await uow.Permissions.GetByRoleNameAsync(user.Role, ct));
        var tokenResponse = new AuthTokenResponse(
            AccessToken: jwtService.GenerateAccessToken(user, permissions),
            RefreshToken: refreshToken.Token,
            ExpiresAt: jwtService.GetAccessTokenExpiry(),
            User: AuthManager.MapToResponse(user));

        return new SsoCallbackResult(tokenResponse, redirectUri);
    }

    public IReadOnlyList<SsoProviderInfo> GetAvailableProviders() =>
        ssoService.GetProviders();

    // Non-destructive lookup — used to send the user back to the correct frontend URL on error.
    public Task<string?> GetRedirectUriForErrorAsync(string state, CancellationToken ct = default) =>
        ssoService.PeekRedirectUriAsync(state, ct);

    // Checks for a unique-constraint violation without importing EF Core (Application layer
    // must not depend on Infrastructure). Postgres code 23505 = unique_violation.
    private static bool IsUniqueConstraintViolation(Exception ex) =>
        ex.GetType().Name == "DbUpdateException" &&
        (ex.InnerException?.Message.Contains("23505") == true ||
         ex.InnerException?.Message.Contains("unique") == true ||
         ex.InnerException?.Message.Contains("ix_users_email") == true);
}
