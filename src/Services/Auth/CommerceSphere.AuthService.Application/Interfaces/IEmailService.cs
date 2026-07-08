namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string toName, string token, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string toName, string token, bool isAdmin, CancellationToken ct = default);
    Task SendTemporaryPasswordAsync(string toEmail, string toName, string temporaryPassword, CancellationToken ct = default);
    Task SendOtpAsync(string toEmail, string toName, string otpCode, CancellationToken ct = default);
    Task SendOrderCancelledAsync(string toEmail, string toName, string orderRef, string reason, CancellationToken ct = default);
}
