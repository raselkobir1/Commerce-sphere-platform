namespace CommerceSphere.AuthService.Application.Interfaces;

public interface ITotpService
{
    string GenerateSecret();
    string GetQrCodeUri(string secret, string email, string issuer = "CommerceSphere");
    bool ValidateCode(string secret, string code);
}
