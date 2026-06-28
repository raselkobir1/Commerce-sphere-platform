namespace CommerceSphere.CartService.Domain.Entities;

// One row per order status change — the timeline a customer sees when tracking an order.
public class OrderStatusEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CartId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private OrderStatusEntry() { }

    public static OrderStatusEntry Create(Guid cartId, string status, string? note = null) =>
        new() { CartId = cartId, Status = status, Note = note };
}
