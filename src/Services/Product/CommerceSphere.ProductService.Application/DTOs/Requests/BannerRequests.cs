namespace CommerceSphere.ProductService.Application.DTOs.Requests;

public record CreateBannerRequest(string Title, string? Subtitle, string ImageUrl, string? LinkUrl, int SortOrder = 0);

public record UpdateBannerRequest(string Title, string? Subtitle, string ImageUrl, string? LinkUrl, bool IsActive, int SortOrder = 0);
