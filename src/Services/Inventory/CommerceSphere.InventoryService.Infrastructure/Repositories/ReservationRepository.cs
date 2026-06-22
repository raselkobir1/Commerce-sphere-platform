using CommerceSphere.InventoryService.Domain.Entities;
using CommerceSphere.InventoryService.Domain.Interfaces.Repositories;
using CommerceSphere.InventoryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.InventoryService.Infrastructure.Repositories;

public class ReservationRepository(InventoryDbContext db) : IReservationRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Reservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Reservation?> GetByCartIdAsync(Guid cartId, CancellationToken ct = default) =>
        db.Reservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.CartId == cartId, ct);

    public Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default) =>
        db.Reservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, ct);

    public async Task AddAsync(Reservation reservation, CancellationToken ct = default) =>
        await db.Reservations.AddAsync(reservation, ct);

    public void Update(Reservation reservation) =>
        db.Reservations.Update(reservation);

    public async Task<(IEnumerable<Reservation> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = db.Reservations
            .AsNoTracking()
            .Include(r => r.Items)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
