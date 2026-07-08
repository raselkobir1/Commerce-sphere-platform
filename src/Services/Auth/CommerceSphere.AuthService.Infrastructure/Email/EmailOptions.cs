namespace CommerceSphere.AuthService.Infrastructure.Email;

public class EmailOptions
{
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public bool UseSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@commercesphere.dev";
    public string FromName { get; set; } = "CommerceSphere";
    public string AppBaseUrl { get; set; } = "http://localhost:5000";
    public string AdminPortalUrl { get; set; } = "http://localhost:4200";
    public string ShopPortalUrl { get; set; } = "http://localhost:4300";
}
