using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;

namespace CommerceSphere.ProductService.Application.Interfaces;

public interface IBannerManager
{
    Task<IReadOnlyList<BannerResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BannerResponse> CreateAsync(CreateBannerRequest request, CancellationToken ct = default);
    Task<BannerResponse> UpdateAsync(Guid id, UpdateBannerRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
