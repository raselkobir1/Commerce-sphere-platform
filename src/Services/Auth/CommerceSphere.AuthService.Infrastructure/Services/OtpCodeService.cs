using System.Security.Cryptography;
using CommerceSphere.AuthService.Application.Interfaces;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Services;

public class OtpCodeService(IConnectionMultiplexer redis) : IOtpCodeService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const string Prefix = "otp:";

    public async Task<string> GenerateAndStoreAsync(Guid userId, CancellationToken ct = default)
    {
        // SECURITY: cryptographically-random 6-digit code (Random.Shared is predictable).
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var db = redis.GetDatabase();
        await db.StringSetAsync($"{Prefix}{userId}", code, Ttl);
        return code;
    }

    public async Task<bool> ValidateAndConsumeAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var key = $"{Prefix}{userId}";
        var stored = await db.StringGetAsync(key);

        if (stored.IsNullOrEmpty || stored != code)
            return false;

        // Delete immediately after successful use — OTP is single-use.
        await db.KeyDeleteAsync(key);
        return true;
    }
}
