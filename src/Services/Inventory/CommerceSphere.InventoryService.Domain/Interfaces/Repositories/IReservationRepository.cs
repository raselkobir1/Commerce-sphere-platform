using CommerceSphere.InventoryService.Domain.Entities;

namespace CommerceSphere.InventoryService.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Reservation?> GetByCartIdAsync(Guid cartId, CancellationToken ct = default);
    Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    void Update(Reservation reservation);
    Task<(IEnumerable<Reservation> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default);
}
