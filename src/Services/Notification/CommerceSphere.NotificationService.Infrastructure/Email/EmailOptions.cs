namespace CommerceSphere.NotificationService.Infrastructure.Email;

public class EmailOptions
{
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public bool UseSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@commercesphere.dev";
    public string FromName { get; set; } = "CommerceSphere";
}
