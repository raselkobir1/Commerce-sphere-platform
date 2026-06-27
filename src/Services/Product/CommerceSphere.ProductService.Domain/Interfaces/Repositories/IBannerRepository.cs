using CommerceSphere.ProductService.Domain.Entities;

namespace CommerceSphere.ProductService.Domain.Interfaces.Repositories;

public interface IBannerRepository
{
    Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Banner>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Banner banner, CancellationToken ct = default);
    void Update(Banner banner);
    void Remove(Banner banner);
}
