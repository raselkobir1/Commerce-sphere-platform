using CommerceSphere.NotificationService.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CommerceSphere.NotificationService.Infrastructure.Email;

public class EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _opts = options.Value;

    public Task SendOrderConfirmationAsync(string toEmail, string toName, string orderRef, decimal amount, int itemCount, CancellationToken ct = default)
    {
        var body = $"""
            <h2>Thanks for your order, {toName}!</h2>
            <p>We've received your order <strong>{orderRef}</strong> and it's being processed.</p>
            <ul>
              <li><strong>Total:</strong> ৳{amount:N0}</li>
              <li><strong>Items:</strong> {itemCount}</li>
            </ul>
            <p>We'll email you again when there's an update. Thank you for shopping with CommerceSphere!</p>
            """;
        return SendAsync(toEmail, toName, $"Your CommerceSphere order {orderRef} is confirmed", body, ct);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_opts.SmtpHost, _opts.SmtpPort, _opts.UseSsl, ct);
        if (!string.IsNullOrEmpty(_opts.Username))
            await client.AuthenticateAsync(_opts.Username, _opts.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("Email sent. To: {To}, Subject: {Subject}", toEmail, subject);
    }
}
