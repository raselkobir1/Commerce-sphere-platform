using CommerceSphere.AuthService.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CommerceSphere.AuthService.Infrastructure.Email;

public class EmailService(
    IOptions<EmailOptions> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _opts = options.Value;

    public Task SendEmailVerificationAsync(string toEmail, string toName, string token, CancellationToken ct = default)
    {
        var verifyUrl = $"{_opts.AppBaseUrl.TrimEnd('/')}/api/auth/email/verify/confirm?token={token}";
        var body = $"""
            <h2>Verify your email address</h2>
            <p>Hi {toName},</p>
            <p>Click the link below to verify your email. The link expires in 24 hours.</p>
            <p><a href="{verifyUrl}" style="padding:10px 20px;background:#00C2CB;color:#fff;border-radius:4px;text-decoration:none">Verify Email</a></p>
            <p>Or copy this link: <code>{verifyUrl}</code></p>
            <p>If you did not create an account, you can ignore this email.</p>
            """;
        return SendAsync(toEmail, toName, "Verify your CommerceSphere email", body, ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string token, CancellationToken ct = default)
    {
        var resetUrl = $"{_opts.AppBaseUrl.TrimEnd('/')}/api/auth/password/reset?token={token}";
        var body = $"""
            <h2>Reset your password</h2>
            <p>Hi {toName},</p>
            <p>Click the link below to reset your password. The link expires in 30 minutes.</p>
            <p><a href="{resetUrl}" style="padding:10px 20px;background:#00C2CB;color:#fff;border-radius:4px;text-decoration:none">Reset Password</a></p>
            <p>Or copy this link: <code>{resetUrl}</code></p>
            <p>If you did not request a password reset, you can safely ignore this email.</p>
            """;
        return SendAsync(toEmail, toName, "Reset your CommerceSphere password", body, ct);
    }

    public Task SendOtpAsync(string toEmail, string toName, string otpCode, CancellationToken ct = default)
    {
        var body = $"""
            <h2>Your login code</h2>
            <p>Hi {toName},</p>
            <p>Use the code below to complete your login. It expires in 5 minutes.</p>
            <p style="font-size:36px;font-weight:bold;letter-spacing:8px;color:#0F1B2D">{otpCode}</p>
            <p>If you did not attempt to log in, please change your password immediately.</p>
            """;
        return SendAsync(toEmail, toName, "Your CommerceSphere login code", body, ct);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_opts.SmtpHost, _opts.SmtpPort, _opts.UseSsl, ct);

            if (!string.IsNullOrEmpty(_opts.Username))
                await client.AuthenticateAsync(_opts.Username, _opts.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email sent. To: {To}, Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email. To: {To}, Subject: {Subject}", toEmail, subject);
            throw;
        }
    }
}
