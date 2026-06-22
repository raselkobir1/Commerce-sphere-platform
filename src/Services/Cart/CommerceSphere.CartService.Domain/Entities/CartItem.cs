namespace CommerceSphere.CartService.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;

    private CartItem() { }

    internal static CartItem Create(Guid cartId, Guid productId, string sku, string productName, int quantity, decimal unitPrice)
    {
        return new CartItem
        {
            CartId = cartId,
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            AddedAt = DateTime.UtcNow
        };
    }

    public void UpdateQuantity(int qty)
    {
        Quantity = qty;
        SetUpdated();
    }

    internal void IncrementQuantity(int qty)
    {
        Quantity += qty;
        SetUpdated();
    }
}
