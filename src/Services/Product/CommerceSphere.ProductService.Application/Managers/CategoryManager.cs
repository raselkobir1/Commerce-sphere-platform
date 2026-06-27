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

        await ValidateParentAsync(request.ParentId, null, ct);

        var category = Category.Create(request.Name, request.Description, request.ParentId, request.SortOrder);
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

        await ValidateParentAsync(request.ParentId, id, ct);

        category.Update(request.Name, request.Description, request.IsActive, request.ParentId, request.SortOrder);
        uow.Categories.Update(category);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category updated. CategoryId: {CategoryId}", category.Id);
        return MapToResponse(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        var all = await uow.Categories.GetAllAsync(ct);
        if (all.Any(c => c.ParentId == id))
            throw new BusinessException("This category has sub-categories. Delete or reparent them first.");

        uow.Categories.Remove(category);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category deleted. CategoryId: {CategoryId}", id);
    }

    // Enforces a clean 2-level tree (a parent must exist and be top-level; no self-parent; a
    // category with sub-categories cannot itself become a child).
    private async Task ValidateParentAsync(Guid? parentId, Guid? selfId, CancellationToken ct)
    {
        if (parentId is null) return;
        if (parentId == selfId)
            throw new BusinessException("A category cannot be its own parent.");

        var parent = await uow.Categories.GetByIdAsync(parentId.Value, ct)
            ?? throw new BusinessException("Selected parent category does not exist.");
        if (parent.ParentId is not null)
            throw new BusinessException("Categories support two levels only — the parent must be a top-level category.");

        if (selfId is not null)
        {
            var all = await uow.Categories.GetAllAsync(ct);
            if (all.Any(c => c.ParentId == selfId))
                throw new BusinessException("This category has sub-categories, so it cannot become a child itself.");
        }
    }

    private static CategoryResponse MapToResponse(Category c) =>
        new(c.Id, c.Name, c.Description, c.IsActive, c.ParentId, c.SortOrder, c.CreatedAt, c.UpdatedAt);
}
