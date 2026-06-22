using CommerceSphere.Shared.Contracts.Events.Inventory;

namespace CommerceSphere.InventoryService.Application.Interfaces;

public interface IInventoryEventProducer
{
    Task PublishReservedAsync(InventoryReservedEvent evt, CancellationToken ct = default);
    Task PublishReservationFailedAsync(InventoryReservationFailedEvent evt, CancellationToken ct = default);
    Task PublishReleasedAsync(InventoryReleasedEvent evt, CancellationToken ct = default);
}
