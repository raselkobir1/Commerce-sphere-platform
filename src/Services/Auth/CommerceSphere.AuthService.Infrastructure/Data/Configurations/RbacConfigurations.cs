using CommerceSphere.AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.AuthService.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        // Keys are domain-generated GUIDs (see CartService lesson) — never DB-generated.
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(250).IsRequired();
        builder.Property(r => r.IsSystem).HasColumnName("is_system");
        builder.Property(r => r.IsDefault).HasColumnName("is_default");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("ix_roles_name");

        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(m => m.Key).HasColumnName("key").HasMaxLength(50).IsRequired();
        builder.Property(m => m.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Route).HasColumnName("route").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Icon).HasColumnName("icon").HasMaxLength(20).IsRequired();
        builder.Property(m => m.SortOrder).HasColumnName("sort_order");
        builder.Property(m => m.ParentId).HasColumnName("parent_id");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(m => m.Key).IsUnique().HasDatabaseName("ix_menus_key");
        builder.HasIndex(m => m.ParentId).HasDatabaseName("ix_menus_parent");
    }
}

public class RoleMenuPermissionConfiguration : IEntityTypeConfiguration<RoleMenuPermission>
{
    public void Configure(EntityTypeBuilder<RoleMenuPermission> builder)
    {
        builder.ToTable("role_menu_permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.RoleId).HasColumnName("role_id");
        builder.Property(p => p.MenuId).HasColumnName("menu_id");
        builder.Property(p => p.CanView).HasColumnName("can_view");
        builder.Property(p => p.CanCreate).HasColumnName("can_create");
        builder.Property(p => p.CanEdit).HasColumnName("can_edit");
        builder.Property(p => p.CanDelete).HasColumnName("can_delete");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => new { p.RoleId, p.MenuId }).IsUnique().HasDatabaseName("ix_role_menu");

        builder.HasOne(p => p.Menu)
            .WithMany()
            .HasForeignKey(p => p.MenuId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
