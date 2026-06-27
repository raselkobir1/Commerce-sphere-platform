using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces.Repositories;
using CommerceSphere.ProductService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ProductService.Infrastructure.Repositories;

public class CategoryRepository(ProductDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return db.Categories.AnyAsync(
            c => c.Name.ToLower() == normalized && (excludeId == null || c.Id != excludeId), ct);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(Category category, CancellationToken ct = default) =>
        await db.Categories.AddAsync(category, ct);

    public void Update(Category category) => db.Categories.Update(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
