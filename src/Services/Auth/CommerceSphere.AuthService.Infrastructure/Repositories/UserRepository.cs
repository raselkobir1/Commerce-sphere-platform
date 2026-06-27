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

    public void Remove(User user) =>
        db.Users.Remove(user);

    public Task<int> CountByRoleAsync(string role, CancellationToken ct = default) =>
        db.Users.CountAsync(u => u.Role == role, ct);

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking().OrderBy(u => u.CreatedAt);
        var total = await query.CountAsync(ct);
        var users = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (users, total);
    }

    public async Task<User?> GetByExternalLoginAsync(
        string provider, string externalUserId, CancellationToken ct = default)
    {
        var userId = await db.ExternalLogins
            .Where(e => e.Provider == provider.ToLowerInvariant() && e.ExternalUserId == externalUserId)
            .Select(e => (Guid?)e.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return null;

        return await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default) =>
        await db.ExternalLogins.AddAsync(login, ct);

    public Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(
            u => u.EmailVerificationToken == token && u.EmailVerificationTokenExpiry > DateTime.UtcNow, ct);

    public Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default) =>
        db.Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(
                u => u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow, ct);
}
