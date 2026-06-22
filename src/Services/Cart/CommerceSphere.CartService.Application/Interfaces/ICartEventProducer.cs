using CommerceSphere.Shared.Contracts.Events.Cart;

namespace CommerceSphere.CartService.Application.Interfaces;

public interface ICartEventProducer
{
    Task PublishCartCreatedAsync(CartCreatedEvent @event);
    Task PublishCartUpdatedAsync(CartUpdatedEvent @event);
    Task PublishCartCheckedOutAsync(CartCheckedOutEvent @event);
    Task PublishCartRolledBackAsync(CartRolledBackEvent @event);
}
