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
    // Single name, or a comma-separated list (a parent category sends itself + its children).
    public string? Category { get; init; }
    public string? SearchTerm { get; init; }
    // When true (used by the storefront), only return active + published products.
    public bool PublishedOnly { get; init; }
    // Storefront feed refinements (all optional).
    public decimal? MaxPrice { get; init; }
    public bool InStockOnly { get; init; }
    // "price-asc" | "price-desc" | "name" | "featured" (default).
    public string? SortBy { get; init; }
}

// Bulk publish / unpublish from the admin product list.
public record PublishProductsRequest(IReadOnlyList<Guid> ProductIds, bool Published);
