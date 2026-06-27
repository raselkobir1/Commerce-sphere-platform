namespace CommerceSphere.ProductService.Application.DTOs.Responses;

public record CategoryResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
