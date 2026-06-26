using CommerceSphere.AuthService.Domain.Entities;

namespace CommerceSphere.AuthService.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
    Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    // SSO-specific: find a user by their social identity (provider + external ID from Keycloak sub).
    Task<User?> GetByExternalLoginAsync(string provider, string externalUserId, CancellationToken ct = default);

    // Persist a new ExternalLogin record linking a local user to a social identity.
    Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default);
}
