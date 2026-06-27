namespace CommerceSphere.ProductService.Application.DTOs.Responses;

public record CategoryResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    Guid? ParentId,
    int SortOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
