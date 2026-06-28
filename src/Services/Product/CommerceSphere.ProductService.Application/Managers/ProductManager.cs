using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Models;
using CommerceSphere.Shared.Contracts.Events.Product;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.ProductService.Application.Managers;

public class ProductManager(
    IUnitOfWork uow,
    IProductCacheService cacheService,
    IProductEventProducer eventProducer,
    ILogger<ProductManager> logger) : IProductManager
{
    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request, string correlationId, CancellationToken ct = default)
    {
        // Normalise SKU to UPPER-CASE so "abc-001" and "ABC-001" are treated as the same SKU
        // regardless of what the caller sends.
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();

        if (await uow.Products.ExistsBySkuAsync(normalizedSku, ct))
            throw new ConflictException($"A product with SKU '{normalizedSku}' already exists.");

        var product = Product.Create(
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.Category,
            request.ImageUrl,
            request.InitialStock);

        await uow.Products.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Product created. ProductId: {ProductId}, SKU: {Sku}, CorrelationId: {CorrelationId}",
            product.Id, product.Sku, correlationId);

        // Publish event AFTER saving so the product ID is already in the DB before
        // Inventory Service tries to create a matching InventoryItem.
        var evt = new ProductCreatedEvent(
            ProductId: product.Id,
            Name: product.Name,
            Sku: product.Sku,
            Price: product.Price,
            InitialStock: product.Stock,
            CorrelationId: correlationId,
            OccurredAt: DateTime.UtcNow);

        await eventProducer.PublishProductCreatedAsync(evt, ct);

        var response = MapToResponse(product);
        await cacheService.SetProductAsync(response, ct);

        return response;
    }

    public async Task<ProductResponse> UpdateAsync(
        Guid id, UpdateProductRequest request, string correlationId, CancellationToken ct = default)
    {
        var product = await uow.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Update(request.Name, request.Description, request.Price, request.Category, request.ImageUrl);

        uow.Products.Update(product);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Product updated. ProductId: {ProductId}, CorrelationId: {CorrelationId}",
            product.Id, correlationId);

        var evt = new ProductUpdatedEvent(
            ProductId: product.Id,
            Name: product.Name,
            Price: product.Price,
            IsActive: product.IsActive,
            CorrelationId: correlationId,
            OccurredAt: DateTime.UtcNow);

        await eventProducer.PublishProductUpdatedAsync(evt, ct);

        await cacheService.RemoveProductAsync(id, ct);

        var response = MapToResponse(product);
        await cacheService.SetProductAsync(response, ct);

        return response;
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await cacheService.GetProductAsync(id, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for ProductId: {ProductId}", id);
            return cached;
        }

        var product = await uow.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        var response = MapToResponse(product);
        await cacheService.SetProductAsync(response, ct);

        return response;
    }

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(
        GetProductsRequest request, CancellationToken ct = default)
    {
        var (products, total) = await uow.Products.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Category,
            request.SearchTerm,
            request.PublishedOnly,
            request.MaxPrice,
            request.InStockOnly,
            request.SortBy,
            ct);

        return PagedResult<ProductResponse>.Create(
            products.Select(MapToResponse),
            total,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<ProductResponse> ActivateAsync(
        Guid id, string correlationId, CancellationToken ct = default)
    {
        var product = await uow.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Activate();
        uow.Products.Update(product);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Product activated. ProductId: {ProductId}, CorrelationId: {CorrelationId}",
            product.Id, correlationId);

        var evt = new ProductUpdatedEvent(
            ProductId: product.Id,
            Name: product.Name,
            Price: product.Price,
            IsActive: product.IsActive,
            CorrelationId: correlationId,
            OccurredAt: DateTime.UtcNow);

        await eventProducer.PublishProductUpdatedAsync(evt, ct);

        await cacheService.RemoveProductAsync(id, ct);

        var response = MapToResponse(product);
        await cacheService.SetProductAsync(response, ct);

        return response;
    }

    public async Task<ProductResponse> DeactivateAsync(
        Guid id, string correlationId, CancellationToken ct = default)
    {
        var product = await uow.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Deactivate();
        uow.Products.Update(product);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Product deactivated. ProductId: {ProductId}, CorrelationId: {CorrelationId}",
            product.Id, correlationId);

        var evt = new ProductUpdatedEvent(
            ProductId: product.Id,
            Name: product.Name,
            Price: product.Price,
            IsActive: product.IsActive,
            CorrelationId: correlationId,
            OccurredAt: DateTime.UtcNow);

        await eventProducer.PublishProductUpdatedAsync(evt, ct);

        await cacheService.RemoveProductAsync(id, ct);

        var response = MapToResponse(product);
        await cacheService.SetProductAsync(response, ct);

        return response;
    }

    public async Task<int> PublishProductsAsync(PublishProductsRequest request, string correlationId, CancellationToken ct = default)
    {
        var count = 0;
        foreach (var id in request.ProductIds.Distinct())
        {
            var product = await uow.Products.GetByIdAsync(id, ct);
            if (product is null) continue;
            if (request.Published) product.Publish(); else product.Unpublish();
            uow.Products.Update(product);
            count++;
        }
        await uow.SaveChangesAsync(ct);

        foreach (var id in request.ProductIds)
            await cacheService.RemoveProductAsync(id, ct);

        logger.LogInformation("Bulk {Action} {Count} product(s). CorrelationId: {CorrelationId}",
            request.Published ? "published" : "unpublished", count, correlationId);
        return count;
    }

    private static ProductResponse MapToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Sku, p.Price, p.Category, p.ImageUrl, p.IsActive, p.IsPublished, p.Stock, p.CreatedAt, p.UpdatedAt);
}
