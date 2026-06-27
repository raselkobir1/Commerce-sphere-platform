using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces.Repositories;
using CommerceSphere.ProductService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ProductService.Infrastructure.Repositories;

public class BannerRepository(ProductDbContext db) : IBannerRepository
{
    public Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Banners.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Banner>> GetAllAsync(CancellationToken ct = default) =>
        await db.Banners.AsNoTracking().OrderBy(b => b.SortOrder).ThenByDescending(b => b.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Banner banner, CancellationToken ct = default) =>
        await db.Banners.AddAsync(banner, ct);

    public void Update(Banner banner) => db.Banners.Update(banner);

    public void Remove(Banner banner) => db.Banners.Remove(banner);
}
