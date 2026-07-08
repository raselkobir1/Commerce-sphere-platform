using CommerceSphere.AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.AuthService.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");

        // Email verification
        builder.Property(u => u.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
        builder.Property(u => u.EmailVerificationToken).HasColumnName("email_verification_token").HasMaxLength(128);
        builder.Property(u => u.EmailVerificationTokenExpiry).HasColumnName("email_verification_token_expiry");

        // Two-factor auth
        builder.Property(u => u.IsActiveTwoFactor).HasColumnName("is_active_two_factor").HasDefaultValue(false);
        builder.Property(u => u.TwoFactorSecret).HasColumnName("two_factor_secret").HasMaxLength(256);
        builder.Property(u => u.TwoFactorConfirmed).HasColumnName("two_factor_confirmed").HasDefaultValue(false);

        // OTP auth
        builder.Property(u => u.IsOtpAuthEnable).HasColumnName("is_otp_auth_enable").HasDefaultValue(false);

        // Password reset
        builder.Property(u => u.PasswordResetToken).HasColumnName("password_reset_token").HasMaxLength(128);
        builder.Property(u => u.PasswordResetTokenExpiry).HasColumnName("password_reset_token_expiry");
        builder.Property(u => u.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(false);

        // Account lockout
        builder.Property(u => u.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
        builder.Property(u => u.LockoutEnd).HasColumnName("lockout_end");

        builder.Property(u => u.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");
        builder.HasIndex(u => u.EmailVerificationToken).HasDatabaseName("ix_users_email_verification_token");
        builder.HasIndex(u => u.PasswordResetToken).HasDatabaseName("ix_users_password_reset_token");

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
