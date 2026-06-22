using CommerceSphere.Shared.Contracts.Events.Product;

namespace CommerceSphere.ProductService.Application.Interfaces;

public interface IProductEventProducer
{
    Task PublishProductCreatedAsync(ProductCreatedEvent evt, CancellationToken ct = default);
    Task PublishProductUpdatedAsync(ProductUpdatedEvent evt, CancellationToken ct = default);
}
