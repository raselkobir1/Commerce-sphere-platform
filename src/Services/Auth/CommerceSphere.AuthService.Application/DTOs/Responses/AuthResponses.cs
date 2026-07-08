namespace CommerceSphere.AuthService.Application.DTOs.Responses;

public record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserResponse User
);

public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    bool EmailVerified,
    bool IsActiveTwoFactor,
    bool TwoFactorConfirmed,
    bool IsOtpAuthEnable,
    bool MustChangePassword,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

// Returned by Login when a second factor is required instead of immediate token issuance.
public abstract record LoginResult;
public sealed record LoginSucceeded(AuthTokenResponse Tokens) : LoginResult;
public sealed record LoginNeedsTwoFactor(string ChallengeToken) : LoginResult;
public sealed record LoginNeedsOtp(string ChallengeToken) : LoginResult;
public sealed record LoginNeedsPasswordChange(string ChallengeToken) : LoginResult;

public record TwoFactorSetupResponse(
    string SecretKey,
    string QrCodeUri,          // otpauth:// URI — scan with Google Authenticator / Authy
    string[] ManualEntrySegments  // 4-character groups for manual entry
);

public record SessionResponse(
    Guid Id,
    string CreatedByIp,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsActive
);
