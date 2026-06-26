using CommerceSphere.AuthService.Application.Interfaces;
using OtpNet;

namespace CommerceSphere.AuthService.Infrastructure.Services;

public class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GetQrCodeUri(string secret, string email, string issuer = "CommerceSphere")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedSecret = Uri.EscapeDataString(secret);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={encodedSecret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return false;

        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key);

        // Allow ±1 time-step window to account for clock drift between client and server.
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }
}
