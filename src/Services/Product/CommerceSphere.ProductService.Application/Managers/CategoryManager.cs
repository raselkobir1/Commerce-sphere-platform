using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.ProductService.Application.Managers;

public class CategoryManager(IUnitOfWork uow, ILogger<CategoryManager> logger) : ICategoryManager
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await uow.Categories.GetAllAsync(ct);
        return categories.Select(MapToResponse).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (await uow.Categories.ExistsByNameAsync(request.Name, null, ct))
            throw new ConflictException($"A category named '{request.Name.Trim()}' already exists.");

        var category = Category.Create(request.Name, request.Description);
        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category created. CategoryId: {CategoryId}, Name: {Name}", category.Id, category.Name);
        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        if (await uow.Categories.ExistsByNameAsync(request.Name, id, ct))
            throw new ConflictException($"A category named '{request.Name.Trim()}' already exists.");

        category.Update(request.Name, request.Description, request.IsActive);
        uow.Categories.Update(category);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category updated. CategoryId: {CategoryId}", category.Id);
        return MapToResponse(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        uow.Categories.Remove(category);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category deleted. CategoryId: {CategoryId}", id);
    }

    private static CategoryResponse MapToResponse(Category c) =>
        new(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
