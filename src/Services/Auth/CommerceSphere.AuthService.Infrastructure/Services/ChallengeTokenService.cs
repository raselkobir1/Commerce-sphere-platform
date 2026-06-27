using System.Security.Cryptography;
using System.Text.Json;
using CommerceSphere.AuthService.Application.Interfaces;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Services;

public class ChallengeTokenService(IConnectionMultiplexer redis) : IChallengeTokenService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const string Prefix = "challenge:";

    public async Task<string> CreateAsync(Guid userId, ChallengeType type, CancellationToken ct = default)
    {
        // SECURITY: CSPRNG bearer token (this token alone completes the 2FA/OTP step of login).
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var payload = JsonSerializer.Serialize(new ChallengePayload(userId, type));
        var db = redis.GetDatabase();
        await db.StringSetAsync($"{Prefix}{token}", payload, Ttl);
        return token;
    }

    public async Task<(Guid UserId, ChallengeType Type)?> ValidateAndConsumeAsync(
        string token, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var key = $"{Prefix}{token}";
        var raw = await db.StringGetAsync(key);

        if (raw.IsNullOrEmpty)
            return null;

        await db.KeyDeleteAsync(key);

        var payload = JsonSerializer.Deserialize<ChallengePayload>(raw!);
        return payload is null ? null : (payload.UserId, payload.Type);
    }

    private record ChallengePayload(Guid UserId, ChallengeType Type);
}
