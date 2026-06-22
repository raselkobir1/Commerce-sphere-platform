using CommerceSphere.Shared.Common.Exceptions;

namespace CommerceSphere.InventoryService.Domain.Entities;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Released,
    Cancelled
}

public class ReservationItem
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public class Reservation : BaseEntity
{
    private readonly List<ReservationItem> _items = [];

    public Guid CartId { get; private set; }
    public Guid UserId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public IReadOnlyList<ReservationItem> Items => _items.AsReadOnly();

    private Reservation() { }

    public static Reservation Create(
        Guid cartId,
        Guid userId,
        string idempotencyKey,
        IEnumerable<ReservationItem> items)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new BusinessException("Idempotency key cannot be empty.");

        var reservation = new Reservation
        {
            CartId = cartId,
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            Status = ReservationStatus.Pending
        };

        reservation._items.AddRange(items);
        return reservation;
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Pending)
            throw new BusinessException($"Cannot confirm a reservation in '{Status}' status.");

        Status = ReservationStatus.Confirmed;
        SetUpdated();
    }

    public void Release()
    {
        if (Status == ReservationStatus.Released)
            throw new BusinessException("Reservation is already released.");
        if (Status == ReservationStatus.Cancelled)
            throw new BusinessException("Cannot release a cancelled reservation.");

        Status = ReservationStatus.Released;
        SetUpdated();
    }

    public void Cancel()
    {
        if (Status == ReservationStatus.Cancelled)
            throw new BusinessException("Reservation is already cancelled.");
        if (Status == ReservationStatus.Released)
            throw new BusinessException("Cannot cancel a released reservation.");

        Status = ReservationStatus.Cancelled;
        SetUpdated();
    }
}
