using CommerceSphere.ProductService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.ProductService.Infrastructure.Data.Configurations;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("banners");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(b => b.Subtitle).HasColumnName("subtitle").HasMaxLength(300).IsRequired();
        builder.Property(b => b.ImageUrl).HasColumnName("image_url").HasMaxLength(1000).IsRequired();
        builder.Property(b => b.LinkUrl).HasColumnName("link_url").HasMaxLength(1000).IsRequired();
        builder.Property(b => b.IsActive).HasColumnName("is_active");
        builder.Property(b => b.SortOrder).HasColumnName("sort_order");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(b => b.SortOrder).HasDatabaseName("ix_banners_sort_order");

        // Same xmin optimistic-concurrency mapping as Product/Category.
        builder.Property(b => b.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
