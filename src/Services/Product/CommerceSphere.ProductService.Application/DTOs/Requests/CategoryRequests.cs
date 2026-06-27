namespace CommerceSphere.ProductService.Application.DTOs.Requests;

public record CreateCategoryRequest(string Name, string? Description, Guid? ParentId, int SortOrder = 0);

public record UpdateCategoryRequest(string Name, string? Description, bool IsActive, Guid? ParentId, int SortOrder = 0);
