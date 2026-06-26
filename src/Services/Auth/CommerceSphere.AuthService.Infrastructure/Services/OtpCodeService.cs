using CommerceSphere.AuthService.Application.Interfaces;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Services;

public class OtpCodeService(IConnectionMultiplexer redis) : IOtpCodeService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const string Prefix = "otp:";

    public async Task<string> GenerateAndStoreAsync(Guid userId, CancellationToken ct = default)
    {
        var code = Random.Shared.Next(100_000, 999_999).ToString();
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
