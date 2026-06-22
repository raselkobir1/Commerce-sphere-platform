using CommerceSphere.Shared.Common.Exceptions;

namespace CommerceSphere.InventoryService.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    private InventoryItem() { }

    public static InventoryItem Create(Guid productId, string sku, int quantityOnHand, int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new BusinessException("SKU cannot be empty.");
        if (quantityOnHand < 0)
            throw new BusinessException("Initial quantity on hand cannot be negative.");
        if (reorderLevel < 0)
            throw new BusinessException("Reorder level cannot be negative.");

        return new InventoryItem
        {
            ProductId = productId,
            Sku = sku.Trim().ToUpperInvariant(),
            QuantityOnHand = quantityOnHand,
            QuantityReserved = 0,
            ReorderLevel = reorderLevel,
            IsActive = true
        };
    }

    public void Reserve(int qty)
    {
        if (qty <= 0)
            throw new BusinessException("Reserve quantity must be greater than zero.");
        if (QuantityAvailable < qty)
            throw new BusinessException(
                $"Insufficient stock for SKU '{Sku}'. Available: {QuantityAvailable}, Requested: {qty}.");

        QuantityReserved += qty;
        SetUpdated();
    }

    public void Release(int qty)
    {
        if (qty <= 0)
            throw new BusinessException("Release quantity must be greater than zero.");

        QuantityReserved = Math.Max(0, QuantityReserved - qty);
        SetUpdated();
    }

    public void ReceiveStock(int qty)
    {
        if (qty <= 0)
            throw new BusinessException("Receive quantity must be greater than zero.");

        QuantityOnHand += qty;
        SetUpdated();
    }

    public void AdjustStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new BusinessException("Stock quantity cannot be negative.");

        QuantityOnHand = newQuantity;
        SetUpdated();
    }
}
