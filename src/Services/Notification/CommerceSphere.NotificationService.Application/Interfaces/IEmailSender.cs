namespace CommerceSphere.NotificationService.Application.Interfaces;

// Sends customer emails. Implemented in Infrastructure over SMTP (MailKit / Mailpit in dev).
public interface IEmailSender
{
    Task SendOrderConfirmationAsync(string toEmail, string toName, string orderRef, decimal amount, int itemCount, CancellationToken ct = default);
}
