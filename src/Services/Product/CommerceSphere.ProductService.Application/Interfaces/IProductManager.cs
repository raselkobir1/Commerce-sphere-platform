using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;
using CommerceSphere.Shared.Common.Models;

namespace CommerceSphere.ProductService.Application.Interfaces;

public interface IProductManager
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request, string correlationId, CancellationToken ct = default);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, string correlationId, CancellationToken ct = default);
    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductResponse>> GetPagedAsync(GetProductsRequest request, CancellationToken ct = default);
    Task<ProductResponse> ActivateAsync(Guid id, string correlationId, CancellationToken ct = default);
    Task<ProductResponse> DeactivateAsync(Guid id, string correlationId, CancellationToken ct = default);
}
