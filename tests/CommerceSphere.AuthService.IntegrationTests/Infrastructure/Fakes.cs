using System.Collections.Concurrent;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Contracts.Events.Auth;

namespace CommerceSphere.AuthService.IntegrationTests.Infrastructure;

// Captures everything the auth flows would "send" so tests can read verification tokens,
// reset tokens, and OTP codes without a real SMTP server.
public sealed class FakeEmailService : IEmailService
{
    public ConcurrentDictionary<string, string> VerificationTokens { get; } = new();
    public ConcurrentDictionary<string, string> ResetTokens { get; } = new();
    public ConcurrentDictionary<string, string> TemporaryPasswords { get; } = new();
    public ConcurrentDictionary<string, string> OtpCodes { get; } = new();
    public ConcurrentDictionary<string, string> CancelledOrders { get; } = new();

    public Task SendEmailVerificationAsync(string toEmail, string toName, string token, CancellationToken ct = default)
    {
        VerificationTokens[toEmail.ToLowerInvariant()] = token;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string token, bool isAdmin, CancellationToken ct = default)
    {
        ResetTokens[toEmail.ToLowerInvariant()] = token;
        return Task.CompletedTask;
    }

    public Task SendTemporaryPasswordAsync(string toEmail, string toName, string temporaryPassword, CancellationToken ct = default)
    {
        TemporaryPasswords[toEmail.ToLowerInvariant()] = temporaryPassword;
        return Task.CompletedTask;
    }

    public Task SendOtpAsync(string toEmail, string toName, string otpCode, CancellationToken ct = default)
    {
        OtpCodes[toEmail.ToLowerInvariant()] = otpCode;
        return Task.CompletedTask;
    }

    public Task SendOrderCancelledAsync(string toEmail, string toName, string orderRef, string reason, CancellationToken ct = default)
    {
        CancelledOrders[toEmail.ToLowerInvariant()] = orderRef;
        return Task.CompletedTask;
    }
}

// No-op Kafka producer so registration doesn't require a broker.
public sealed class FakeUserEventProducer : IUserEventProducer
{
    public List<UserCreatedEvent> Published { get; } = [];

    public Task PublishUserCreatedAsync(UserCreatedEvent evt, CancellationToken ct = default)
    {
        Published.Add(evt);
        return Task.CompletedTask;
    }
}

// Stub SSO so the providers/login endpoints work without calling out to real OAuth providers.
public sealed class FakeSsoService : ISsoService
{
    public Task<SsoLoginUrlResponse> BuildLoginUrlAsync(string provider, string redirectUri, CancellationToken ct = default)
        => Task.FromResult(new SsoLoginUrlResponse(provider, $"https://sso.test/auth?provider={provider}", "state-token"));

    public Task<(SsoUserInfo UserInfo, string Provider, string RedirectUri)> ProcessCallbackAsync(
        string code, string state, CancellationToken ct = default)
        => throw new NotImplementedException("SSO callback is not exercised in integration tests.");

    public IReadOnlyList<SsoProviderInfo> GetProviders() => new[]
    {
        new SsoProviderInfo("google", true),
        new SsoProviderInfo("facebook", true),
    };

    public Task<string?> PeekRedirectUriAsync(string state, CancellationToken ct = default)
        => Task.FromResult<string?>("http://localhost/callback");
}
