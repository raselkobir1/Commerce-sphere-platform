using CommerceSphere.Shared.Common.Models;

namespace CommerceSphere.ProductService.Application.DTOs.Requests;

public record CreateProductRequest(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Category,
    string? ImageUrl,
    int InitialStock
);

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string Category,
    string? ImageUrl
);

public class GetProductsRequest : PagedRequest
{
    public string? Category { get; init; }
    public string? SearchTerm { get; init; }
}
