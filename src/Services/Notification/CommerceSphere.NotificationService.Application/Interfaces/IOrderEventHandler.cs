using CommerceSphere.Shared.Contracts.Events.Auth;
using CommerceSphere.Shared.Contracts.Events.Cart;

namespace CommerceSphere.NotificationService.Application.Interfaces;

// Handles the domain events the notification service reacts to. Each method is idempotent —
// safe to call again for a redelivered Kafka message.
public interface IOrderEventHandler
{
    Task HandleCheckedOutAsync(CartCheckedOutEvent evt, CancellationToken ct = default);
    Task HandleCancelledAsync(CartCancelledEvent evt, CancellationToken ct = default);
    Task HandleUserCreatedAsync(UserCreatedEvent evt, CancellationToken ct = default);
}
