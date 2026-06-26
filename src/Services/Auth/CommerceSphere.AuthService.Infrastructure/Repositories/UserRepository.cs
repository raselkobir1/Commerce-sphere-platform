using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces.Repositories;
using CommerceSphere.AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.AuthService.Infrastructure.Repositories;

public class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public void Update(User user) =>
        db.Users.Update(user);

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking().OrderBy(u => u.CreatedAt);
        var total = await query.CountAsync(ct);
        var users = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (users, total);
    }

    // SSO: find a local user via their social identity.
    // Joins through ExternalLogins so we get the full User entity including RefreshTokens.
    public Task<User?> GetByExternalLoginAsync(
        string provider, string externalUserId, CancellationToken ct = default) =>
        db.ExternalLogins
            .Where(e => e.Provider == provider.ToLowerInvariant() && e.ExternalUserId == externalUserId)
            .Select(e => e.User)
            .Include(u => u!.RefreshTokens)
            .FirstOrDefaultAsync(ct);

    // SSO: persist a new ExternalLogin row linking a local user to their social account.
    public async Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default) =>
        await db.ExternalLogins.AddAsync(login, ct);
}
