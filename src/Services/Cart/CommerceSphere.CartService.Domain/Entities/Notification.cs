namespace CommerceSphere.CartService.Domain.Entities;

// An admin-facing notification (e.g. a new order was placed). Persisted so the unread count
// survives browser refreshes; pushed live to the admin panel via SignalR when created.
public class Notification : BaseEntity
{
    public string Type { get; private set; } = string.Empty;   // e.g. "OrderPlaced"
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }                   // the cart/order this refers to
    public Guid UserId { get; private set; }                    // the customer who placed it
    public decimal Amount { get; private set; }
    public int ItemCount { get; private set; }
    public bool IsRead { get; private set; }

    private Notification() { }

    public static Notification OrderPlaced(Guid orderId, Guid userId, decimal amount, int itemCount)
    {
        var reference = orderId.ToString("N")[..8].ToUpperInvariant();
        return new Notification
        {
            Type = "OrderPlaced",
            Title = "New order placed",
            Message = $"Order #{reference} · ৳{amount:N0} · {itemCount} item(s)",
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
