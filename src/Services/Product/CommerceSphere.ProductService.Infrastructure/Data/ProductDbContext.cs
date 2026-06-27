using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ProductService.Infrastructure.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Banner> Banners => Set<Banner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new BannerConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
