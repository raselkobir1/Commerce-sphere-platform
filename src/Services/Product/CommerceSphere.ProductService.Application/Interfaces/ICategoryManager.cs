using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;

namespace CommerceSphere.ProductService.Application.Interfaces;

public interface ICategoryManager
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
