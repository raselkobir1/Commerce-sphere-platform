namespace CommerceSphere.NotificationService.Domain.Entities;

// An admin-facing notification (e.g. a new order placed / cancelled). Persisted so the unread
// count survives restarts; pushed live to the admin panel via SignalR when created.
public class Notification : BaseEntity
{
    public string Type { get; private set; } = string.Empty;   // "OrderPlaced" | "OrderCancelled"
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }                   // the order this refers to
    public Guid UserId { get; private set; }                    // the customer
    public decimal Amount { get; private set; }
    public int ItemCount { get; private set; }
    public bool IsRead { get; private set; }

    private Notification() { }

    public static Notification OrderPlaced(Guid orderId, Guid userId, decimal amount, int itemCount) =>
        Build("OrderPlaced", "New order placed", orderId, userId, amount, itemCount);

    public static Notification OrderCancelled(Guid orderId, Guid userId, decimal amount, int itemCount) =>
        Build("OrderCancelled", "Order cancelled", orderId, userId, amount, itemCount);

    private static Notification Build(string type, string title, Guid orderId, Guid userId, decimal amount, int itemCount)
    {
        var reference = orderId.ToString("N")[..8].ToUpperInvariant();
        var verb = type == "OrderCancelled" ? "cancelled" : "placed";
        return new Notification
        {
            Type = type,
            Title = title,
            Message = $"Order #{reference} {verb} · ৳{amount:N0} · {itemCount} item(s)",
            OrderId = orderId,
            UserId = userId,
            Amount = amount,
            ItemCount = itemCount,
            IsRead = false,
        };
    }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        SetUpdated();
    }
}
