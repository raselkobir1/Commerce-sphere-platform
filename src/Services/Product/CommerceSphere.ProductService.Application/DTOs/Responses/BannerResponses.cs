namespace CommerceSphere.ProductService.Application.DTOs.Responses;

public record BannerResponse(
    Guid Id,
    string Title,
    string Subtitle,
    string ImageUrl,
    string LinkUrl,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
