using CommerceSphere.NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.NotificationService.Infrastructure.Data.Configurations;

public class UserContactConfiguration : IEntityTypeConfiguration<UserContact>
{
    public void Configure(EntityTypeBuilder<UserContact> builder)
    {
        builder.ToTable("user_contacts");
        builder.HasKey(c => c.UserId);

        builder.Property(c => c.UserId).HasColumnName("user_id").ValueGeneratedNever();
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(c => c.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
    }
}
