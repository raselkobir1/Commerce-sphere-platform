namespace CommerceSphere.ProductService.Application.DTOs.Responses;

public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Category,
    string? ImageUrl,
    bool IsActive,
    bool IsPublished,
    int Stock,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
