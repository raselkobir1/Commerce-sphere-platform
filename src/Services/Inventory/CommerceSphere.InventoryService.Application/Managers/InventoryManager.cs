using CommerceSphere.InventoryService.Application.DTOs.Requests;
using CommerceSphere.InventoryService.Application.DTOs.Responses;
using CommerceSphere.InventoryService.Application.Interfaces;
using CommerceSphere.InventoryService.Domain.Entities;
using CommerceSphere.InventoryService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Idempotency;
using CommerceSphere.Shared.Common.Locking;
using CommerceSphere.Shared.Common.Models;
using CommerceSphere.Shared.Contracts.Events.Inventory;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.InventoryService.Application.Managers;

public class InventoryManager(
    IUnitOfWork uow,
    IInventoryCacheService cacheService,
    IInventoryEventProducer eventProducer,
    IIdempotencyService idempotencyService,
    IDistributedLockService lockService,
    ILogger<InventoryManager> logger) : IInventoryManager
{
    // How long a stock lock may be held before it auto-expires (covers one reservation's DB round trip).
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
    // How long to wait for a contended lock before giving up.
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(5);

    private static string StockLockKey(Guid productId) => $"inventory:{productId}";

    public async Task<ReservationResponse> ReserveAsync(
        ReserveInventoryRequest request, string correlationId, CancellationToken ct = default)
    {
        // Atomically claim the idempotency key (short TTL): closes the check-then-mark race a plain
        // IsProcessedAsync/MarkProcessedAsync pair leaves open. Re-marked with a long TTL on success
        // below; on failure the short claim simply expires so a legitimate retry isn't blocked.
        if (!await idempotencyService.TryMarkProcessedAsync(request.IdempotencyKey, TimeSpan.FromMinutes(1), ct))
            throw new IdempotencyException(request.IdempotencyKey);

        // Lock every SKU being reserved (sorted order, so concurrent multi-item reservations can
        // never deadlock on each other) — prevents two concurrent reservations from both reading the
        // same stale QuantityAvailable and over-committing stock.
        var lockKeys = request.Items.Select(i => StockLockKey(i.ProductId));
        await using var stockLock = await lockService.AcquireAllAsync(lockKeys, LockExpiry, LockWait, ct)
            ?? throw new BusinessException("Inventory is busy processing another update for one of these items. Please retry.");

        try
        {
            // Wrap all stock deductions in a DB transaction: if any single SKU is out of stock,
            // the whole reservation rolls back — partial reservations are never committed.
            var reservation = await uow.ExecuteInTransactionAsync(async () =>
            {
                var reservationItems = new List<ReservationItem>();

                // Reserve stock for each item in the request
                foreach (var item in request.Items)
                {
                    var inventoryItem = await uow.Inventory.GetByProductIdAsync(item.ProductId, ct)
                        ?? throw new NotFoundException(nameof(InventoryItem), item.ProductId);

                    inventoryItem.Reserve(item.Quantity);
                    uow.Inventory.Update(inventoryItem);

                    reservationItems.Add(new ReservationItem
                    {
                        ProductId = item.ProductId,
                        Sku = item.Sku,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }

                var newReservation = Reservation.Create(
                    request.CartId,
                    request.UserId,
                    request.IdempotencyKey,
                    reservationItems);

                await uow.Reservations.AddAsync(newReservation, ct);
                await uow.SaveChangesAsync(ct);
                return newReservation;
            }, ct);

            logger.LogInformation(
                "Inventory reserved. ReservationId: {ReservationId}, CartId: {CartId}, CorrelationId: {CorrelationId}",
                reservation.Id, request.CartId, correlationId);

            // Publish event
            var evt = new InventoryReservedEvent(
                ReservationId: reservation.Id,
                CartId: request.CartId,
                UserId: request.UserId,
                Items: reservation.Items.Select(i =>
                    new ReservedItem(i.ProductId, i.Sku, i.Quantity, i.UnitPrice)).ToList(),
                CorrelationId: correlationId,
                OccurredAt: DateTime.UtcNow);

            await eventProducer.PublishReservedAsync(evt, ct);

            // Mark idempotency key as processed
            await idempotencyService.MarkProcessedAsync(request.IdempotencyKey, TimeSpan.FromHours(24), ct);

            // Invalidate cache for affected products
            foreach (var item in request.Items)
                await cacheService.RemoveInventoryAsync(item.ProductId, ct);

            return MapToReservationResponse(reservation);
        }
        catch (Exception ex) when (ex is not IdempotencyException)
        {
            // IdempotencyException is excluded from this catch so it propagates to the caller
            // without publishing a failure event (the request was already handled). The transaction
            // itself is already rolled back by ExecuteInTransactionAsync.
            logger.LogWarning(ex,
                "Inventory reservation failed. CartId: {CartId}, CorrelationId: {CorrelationId}",
                request.CartId, correlationId);

            // Publish the failure event so the Cart Service saga consumer can roll back the cart.
            var failedEvt = new InventoryReservationFailedEvent(
                CartId: request.CartId,
                UserId: request.UserId,
                Reason: ex.Message,
                CorrelationId: correlationId,
                OccurredAt: DateTime.UtcNow);

            await eventProducer.PublishReservationFailedAsync(failedEvt, ct);

            throw;
        }
    }

    public async Task<ReservationResponse> ReleaseAsync(
        ReleaseReservationRequest request, string correlationId, CancellationToken ct = default)
    {
        var reservation = await uow.Reservations.GetByIdAsync(request.ReservationId, ct)
            ?? throw new NotFoundException(nameof(Reservation), request.ReservationId);

        var lockKeys = reservation.Items.Select(i => StockLockKey(i.ProductId));
        await using var stockLock = await lockService.AcquireAllAsync(lockKeys, LockExpiry, LockWait, ct)
            ?? throw new BusinessException("Inventory is busy processing another update for one of these items. Please retry.");

        // Release stock for each item in the reservation
        await uow.ExecuteInTransactionAsync(async () =>
        {
            foreach (var item in reservation.Items)
            {
                var inventoryItem = await uow.Inventory.GetByProductIdAsync(item.ProductId, ct);
                if (inventoryItem is not null)
                {
                    inventoryItem.Release(item.Quantity);
                    uow.Inventory.Update(inventoryItem);
                }
            }

            reservation.Release();
            uow.Reservations.Update(reservation);
            await uow.SaveChangesAsync(ct);
            return true;
        }, ct);

        logger.LogInformation(
            "Reservation released. ReservationId: {ReservationId}, CartId: {CartId}, CorrelationId: {CorrelationId}",
            reservation.Id, request.CartId, correlationId);

        // Publish event
        var evt = new InventoryReleasedEvent(
            ReservationId: reservation.Id,
            CartId: request.CartId,
            Reason: request.Reason,
            CorrelationId: correlationId,
            OccurredAt: DateTime.UtcNow);

        await eventProducer.PublishReleasedAsync(evt, ct);

        // Invalidate cache for affected products
        foreach (var item in reservation.Items)
            await cacheService.RemoveInventoryAsync(item.ProductId, ct);

        return MapToReservationResponse(reservation);
    }

    public async Task<InventoryItemResponse> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        // Cache-aside pattern
        var cached = await cacheService.GetInventoryAsync(productId, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for ProductId: {ProductId}", productId);
            return cached;
        }

        var item = await uow.Inventory.GetByProductIdAsync(productId, ct)
            ?? throw new NotFoundException(nameof(InventoryItem), productId);

        var response = MapToInventoryItemResponse(item);
        await cacheService.SetInventoryAsync(productId, response, ct);

        return response;
    }

    public async Task<PagedResult<InventoryItemResponse>> GetInventoryPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await uow.Inventory.GetPagedAsync(pageNumber, pageSize, ct);

        return PagedResult<InventoryItemResponse>.Create(
            items.Select(MapToInventoryItemResponse),
            total,
            pageNumber,
            pageSize);
    }

    public async Task<InventoryItemResponse> AdjustStockAsync(
        AdjustStockRequest request, string correlationId, CancellationToken ct = default)
    {
        await using var stockLock = await lockService.AcquireAsync(StockLockKey(request.ProductId), LockExpiry, LockWait, ct)
            ?? throw new BusinessException("Inventory is busy processing another update for this item. Please retry.");

        var item = await uow.Inventory.GetByProductIdAsync(request.ProductId, ct)
            ?? throw new NotFoundException(nameof(InventoryItem), request.ProductId);

        item.AdjustStock(request.NewQuantity);
        uow.Inventory.Update(item);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Stock adjusted. ProductId: {ProductId}, NewQuantity: {NewQuantity}, CorrelationId: {CorrelationId}",
            request.ProductId, request.NewQuantity, correlationId);

        await cacheService.RemoveInventoryAsync(request.ProductId, ct);

        return MapToInventoryItemResponse(item);
    }

    public async Task<InventoryItemResponse> ReceiveStockAsync(
        ReceiveStockRequest request, string correlationId, CancellationToken ct = default)
    {
        await using var stockLock = await lockService.AcquireAsync(StockLockKey(request.ProductId), LockExpiry, LockWait, ct)
            ?? throw new BusinessException("Inventory is busy processing another update for this item. Please retry.");

        var item = await uow.Inventory.GetByProductIdAsync(request.ProductId, ct)
            ?? throw new NotFoundException(nameof(InventoryItem), request.ProductId);

        item.ReceiveStock(request.Quantity);
        uow.Inventory.Update(item);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Stock received. ProductId: {ProductId}, Quantity: {Quantity}, CorrelationId: {CorrelationId}",
            request.ProductId, request.Quantity, correlationId);

        await cacheService.RemoveInventoryAsync(request.ProductId, ct);

        return MapToInventoryItemResponse(item);
    }

    private static InventoryItemResponse MapToInventoryItemResponse(InventoryItem item) =>
        new(item.Id, item.ProductId, item.Sku, item.QuantityOnHand, item.QuantityReserved,
            item.QuantityAvailable, item.ReorderLevel, item.IsActive);

    private static ReservationResponse MapToReservationResponse(Reservation r) =>
        new(r.Id, r.CartId, r.UserId, r.Status.ToString(),
            r.Items.Select(i => new ReservationItemResponse(i.ProductId, i.Sku, i.Quantity, i.UnitPrice)).ToList(),
            r.CreatedAt);
}
