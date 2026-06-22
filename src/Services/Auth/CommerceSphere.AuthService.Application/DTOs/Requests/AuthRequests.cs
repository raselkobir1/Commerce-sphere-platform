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
