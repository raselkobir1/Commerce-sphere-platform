namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IOtpCodeService
{
    Task<string> GenerateAndStoreAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ValidateAndConsumeAsync(Guid userId, string code, CancellationToken ct = default);
}
