namespace CommerceSphere.AuthService.Application.DTOs.Requests;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role = "Customer"
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record RevokeTokenRequest(
    string RefreshToken
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UpdateProfileRequest(
    string FirstName,
    string LastName
);

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string Token,
    string NewPassword
);

public record ForcedPasswordChangeRequest(
    string ChallengeToken,
    string NewPassword
);

public record VerifyEmailRequest(
    string Token
);

public record ResendVerificationEmailRequest(
    string Email
);

public record ConfirmTwoFactorRequest(
    string Code
);

public record DisableTwoFactorRequest(
    string Code
);

public record TwoFactorChallengeRequest(
    string ChallengeToken,
    string Code
);

public record OtpChallengeRequest(
    string ChallengeToken,
    string Code
);

public record ToggleOtpRequest(
    bool Enable
);
