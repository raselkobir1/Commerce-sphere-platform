using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.AuthService.Infrastructure.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // SSO-linked identities — each row links a local User to a social provider account.
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalLoginConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
