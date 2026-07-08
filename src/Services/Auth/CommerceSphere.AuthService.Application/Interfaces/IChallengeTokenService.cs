namespace CommerceSphere.AuthService.Application.Interfaces;

public enum ChallengeType { TwoFactor, Otp, PasswordChange }

public interface IChallengeTokenService
{
    Task<string> CreateAsync(Guid userId, ChallengeType type, CancellationToken ct = default);
    Task<(Guid UserId, ChallengeType Type)?> ValidateAndConsumeAsync(string token, CancellationToken ct = default);
}
