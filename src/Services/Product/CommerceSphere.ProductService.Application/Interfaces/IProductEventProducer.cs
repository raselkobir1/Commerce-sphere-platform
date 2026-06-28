using CommerceSphere.Shared.Contracts.Events.Product;

namespace CommerceSphere.ProductService.Application.Interfaces;

public interface IProductEventProducer
{
    Task PublishProductCreatedAsync(ProductCreatedEvent evt, CancellationToken ct = default);
    Task PublishProductUpdatedAsync(ProductUpdatedEvent evt, CancellationToken ct = default);

    // Bulk path: fire many ProductCreatedEvents with non-blocking Produce + a single Flush,
    // instead of awaiting one delivery per product (used by the Excel importer).
    Task PublishProductCreatedBatchAsync(IEnumerable<ProductCreatedEvent> events, CancellationToken ct = default);
}
