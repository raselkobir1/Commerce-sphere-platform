using CommerceSphere.Shared.Common.Exceptions;

namespace CommerceSphere.InventoryService.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int QuantityOnHand { get; private set; }
    // QuantityReserved tracks stock committed to in-flight checkouts but not yet shipped.
    // Keeping it separate from QuantityOnHand means physical stock stays accurate until fulfilment.
    public int QuantityReserved { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; }

    // The quantity a new order can actually claim — the only number that should be shown to buyers.
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

        // Math.Max guards against a reserved count going negative if a release is replayed
        // (e.g. a duplicate Kafka message arrives after a crash and retry).
        QuantityReserved = Math.Max(0, QuantityReserved - qty);
        SetUpdated();
    }

    // A completed sale: physically removes stock on hand (and frees any matching reservation).
    // Clamped at zero so a replayed checkout event can never drive stock negative.
    public void Sell(int qty)
    {
        if (qty <= 0)
            throw new BusinessException("Sell quantity must be greater than zero.");

        QuantityOnHand = Math.Max(0, QuantityOnHand - qty);
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
