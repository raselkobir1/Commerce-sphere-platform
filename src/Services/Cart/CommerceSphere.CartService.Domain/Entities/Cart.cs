namespace CommerceSphere.CartService.Domain.Entities;

public enum CartStatus
{
    Active,
    CheckedOut,
    Abandoned,
    RolledBack,
    Cancelled
}

public class Cart : BaseEntity
{
    // Private backing list — domain logic is the only place that can add/remove items.
    // The public Items property exposes a read-only view so callers cannot mutate the collection directly.
    private readonly List<CartItem> _items = [];

    public Guid UserId { get; private set; }
    public CartStatus Status { get; private set; } = CartStatus.Active;
    public string? IdempotencyKey { get; private set; }
    public ICollection<CartItem> Items => _items.AsReadOnly();

    // Computed on-the-fly from items so it is always consistent with actual cart contents.
    public decimal TotalAmount => _items.Sum(i => i.UnitPrice * i.Quantity);
    public int ItemCount => _items.Count;

    private Cart() { }

    public static Cart Create(Guid userId, string? idempotencyKey = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must not be empty.", nameof(userId));

        return new Cart
        {
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            Status = CartStatus.Active
        };
    }

    public void AddItem(Guid productId, string sku, string productName, int quantity, decimal unitPrice)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            // If the product is already in the cart, increment its quantity rather than
            // adding a duplicate line — keeps the cart clean from the user's perspective.
            existing.IncrementQuantity(quantity);
        }
        else
        {
            var item = CartItem.Create(Id, productId, sku, productName, quantity, unitPrice);
            _items.Add(item);
        }
        SetUpdated();
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null)
        {
            _items.Remove(item);
            SetUpdated();
        }
    }

    public void UpdateItemQuantity(Guid productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException($"CartItem with ProductId '{productId}' was not found.");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        item.UpdateQuantity(quantity);
        SetUpdated();
    }

    public void Checkout()
    {
        Status = CartStatus.CheckedOut;
        SetUpdated();
    }

    public void Rollback(string reason)
    {
        Status = CartStatus.RolledBack;
        SetUpdated();
    }

    public void Abandon()
    {
        Status = CartStatus.Abandoned;
        SetUpdated();
    }

    // Admin cancels a placed order (the manager enforces that only checked-out orders qualify).
    public void Cancel()
    {
        Status = CartStatus.Cancelled;
        SetUpdated();
    }

    public void Clear()
    {
        _items.Clear();
        SetUpdated();
    }
}
