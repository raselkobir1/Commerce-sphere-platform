using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces.Repositories;

namespace CommerceSphere.AuthService.UnitTests.Common;

// In-memory IUnitOfWork so manager tests exercise real read-after-write behaviour
// (register → user exists, refresh-token rotation, session revocation) without a database.
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public FakeUserRepository UsersStore { get; } = new();
    public FakeRefreshTokenRepository RefreshTokensStore { get; } = new();

    public IUserRepository Users => UsersStore;
    public IRefreshTokenRepository RefreshTokens => RefreshTokensStore;

    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Dispose() { }
}

public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = [];
    private readonly List<(string Provider, string ExternalId, Guid UserId)> _externalLogins = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(Items.Any(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        Items.Add(user);
        return Task.CompletedTask;
    }

    // No-op: the in-memory instance is already the tracked reference.
    public void Update(User user) { }

    public Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var page = Items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IEnumerable<User>)page, Items.Count));
    }

    public Task<User?> GetByExternalLoginAsync(string provider, string externalUserId, CancellationToken ct = default)
    {
        var link = _externalLogins.FirstOrDefault(l => l.Provider == provider && l.ExternalId == externalUserId);
        return Task.FromResult(link.UserId == Guid.Empty ? null : Items.FirstOrDefault(u => u.Id == link.UserId));
    }

    public Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default)
    {
        _externalLogins.Add((login.Provider, login.ExternalUserId, login.UserId));
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.EmailVerificationToken == token));

    public Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.PasswordResetToken == token));
}

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = [];

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(t => t.Token == token));

    public Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        Items.Add(token);
        return Task.CompletedTask;
    }

    public void Update(RefreshToken token) { }

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (var t in Items.Where(t => t.UserId == userId && t.IsActive))
            t.Revoke();
        return Task.CompletedTask;
    }
}
