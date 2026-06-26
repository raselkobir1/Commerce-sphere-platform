namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string toName, string token, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string toName, string token, CancellationToken ct = default);
    Task SendOtpAsync(string toEmail, string toName, string otpCode, CancellationToken ct = default);
}
