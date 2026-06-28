using System.Text.Json;
using CommerceSphere.CartService.Application.DTOs.Requests;
using CommerceSphere.CartService.Application.DTOs.Responses;
using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.CartService.Domain.Entities;
using CommerceSphere.CartService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Idempotency;
using CommerceSphere.Shared.Contracts.Events.Cart;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.CartService.Application.Managers;

public class CartManager(
    IUnitOfWork uow,
    ICartCacheService cacheService,
    ICartEventProducer eventProducer,
    IIdempotencyService idempotencyService,
    ILogger<CartManager> logger) : ICartManager
{
    // Kafka topics that go through the transactional outbox (written atomically with the order).
    private const string CartCheckedOutTopic = "cart-checkedout";
    private const string CartCancelledTopic = "cart-cancelled";
    public async Task<CartResponse> CreateCartAsync(CreateCartRequest request, string correlationId, CancellationToken ct = default)
    {
        // Idempotency check
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await uow.Carts.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            if (existing is not null)
            {
                logger.LogInformation("Idempotent request: cart already exists for key {Key}", request.IdempotencyKey);
                return MapToResponse(existing);
            }
        }

        var cart = Cart.Create(request.UserId, request.IdempotencyKey);
        await uow.Carts.AddAsync(cart, ct);
        await uow.SaveChangesAsync(ct);

        await eventProducer.PublishCartCreatedAsync(new CartCreatedEvent(
            cart.Id, cart.UserId, correlationId, DateTime.UtcNow));

        var response = MapToResponse(cart);
        await cacheService.SetCartAsync(cart.Id, response);

        logger.LogInformation("Cart {CartId} created for user {UserId}", cart.Id, cart.UserId);
        return response;
    }

    public async Task<CartResponse> AddItemAsync(Guid cartId, AddCartItemRequest request, string correlationId, CancellationToken ct = default)
    {
        var cart = await GetActiveCartAsync(cartId, ct);

        cart.AddItem(request.ProductId, request.Sku, request.ProductName, request.Quantity, request.UnitPrice);
        // The cart is already change-tracked (loaded via GetByIdAsync), so SaveChanges persists the
        // new item on its own. Do NOT call DbSet.Update() here: it marks the newly-added CartItem
        // (which has a client-generated GUID key) as Modified instead of Added, emitting an UPDATE
        // for a row that doesn't exist → DbUpdateConcurrencyException ("0 rows affected").
        await uow.SaveChangesAsync(ct);

        await eventProducer.PublishCartUpdatedAsync(new CartUpdatedEvent(
            cart.Id, cart.UserId, cart.ItemCount, cart.TotalAmount, correlationId, DateTime.UtcNow));

        var response = MapToResponse(cart);
        await cacheService.SetCartAsync(cart.Id, response);

        logger.LogInformation("Item {ProductId} added to cart {CartId}", request.ProductId, cartId);
        return response;
    }

    public async Task<CartResponse> UpdateItemAsync(Guid cartId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        var cart = await GetActiveCartAsync(cartId, ct);

        cart.UpdateItemQuantity(request.ProductId, request.Quantity);
        // Cart is already change-tracked; SaveChanges persists the change (no DbSet.Update needed).
        await uow.SaveChangesAsync(ct);

        await eventProducer.PublishCartUpdatedAsync(new CartUpdatedEvent(
            cart.Id, cart.UserId, cart.ItemCount, cart.TotalAmount, string.Empty, DateTime.UtcNow));

        var response = MapToResponse(cart);
        await cacheService.SetCartAsync(cart.Id, response);

        logger.LogInformation("Item {ProductId} updated in cart {CartId} to qty {Qty}", request.ProductId, cartId, request.Quantity);
        return response;
    }

    public async Task<CartResponse> RemoveItemAsync(Guid cartId, Guid productId, CancellationToken ct = default)
    {
        var cart = await GetActiveCartAsync(cartId, ct);

        cart.RemoveItem(productId);
        // Cart is already change-tracked; SaveChanges persists the change (no DbSet.Update needed).
        await uow.SaveChangesAsync(ct);

        await eventProducer.PublishCartUpdatedAsync(new CartUpdatedEvent(
            cart.Id, cart.UserId, cart.ItemCount, cart.TotalAmount, string.Empty, DateTime.UtcNow));

        var response = MapToResponse(cart);
        await cacheService.SetCartAsync(cart.Id, response);

        logger.LogInformation("Item {ProductId} removed from cart {CartId}", productId, cartId);
        return response;
    }

    public async Task<CartResponse> GetCartAsync(Guid cartId, CancellationToken ct = default)
    {
        // Cache-aside
        var cached = await cacheService.GetCartAsync(cartId);
        if (cached is not null)
        {
            logger.LogDebug("Cart {CartId} served from cache", cartId);
            return cached;
        }

        var cart = await uow.Carts.GetByIdAsync(cartId, ct)
            ?? throw new NotFoundException(nameof(Cart), cartId);

        var response = MapToResponse(cart);
        await cacheService.SetCartAsync(cartId, response);
        return response;
    }

    public async Task<CartResponse> GetCartByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await uow.Carts.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(Cart), userId);

        return MapToResponse(cart);
    }

    public async Task<IReadOnlyList<CartResponse>> GetOrdersAsync(CancellationToken ct = default)
    {
        var orders = await uow.Carts.GetOrdersAsync(ct);
        return orders.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<CartResponse>> GetUserOrdersAsync(Guid userId, CancellationToken ct = default)
    {
        var orders = await uow.Carts.GetOrdersByUserAsync(userId, ct);
        return orders.Select(MapToResponse).ToList();
    }

    // Admin cancel — may cancel any order.
    public Task<CartResponse> CancelOrderAsync(Guid cartId, string reason, string correlationId, CancellationToken ct = default)
        => CancelInternalAsync(cartId, null, reason, correlationId, ct);

    // Customer cancel — only their own order (ownerUserId is enforced).
    public Task<CartResponse> CancelOwnOrderAsync(Guid cartId, Guid userId, string reason, string correlationId, CancellationToken ct = default)
        => CancelInternalAsync(cartId, userId, reason, correlationId, ct);

    private async Task<CartResponse> CancelInternalAsync(Guid cartId, Guid? requireUserId, string reason, string correlationId, CancellationToken ct)
    {
        var cart = await uow.Carts.GetByIdAsync(cartId, ct)
            ?? throw new NotFoundException(nameof(Cart), cartId);

        // Ownership guard for the customer path — you can only cancel your own orders.
        if (requireUserId is not null && cart.UserId != requireUserId.Value)
            throw new BusinessException("You can only cancel your own orders.");

        if (cart.Status != CartStatus.CheckedOut)
            throw new BusinessException($"Only checked-out orders can be cancelled (current status: {cart.Status}).");

        cart.Cancel();

        // Snapshot items before saving so the event carries what to restock / email about.
        var snapshots = cart.Items
            .Select(i => new CartItemSnapshot(i.ProductId, i.Sku, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList()
            .AsReadOnly();

        var cancelledEvent = new CartCancelledEvent(
            cart.Id, cart.UserId, cart.TotalAmount, snapshots,
            string.IsNullOrWhiteSpace(reason) ? "Cancelled" : reason.Trim(),
            correlationId, DateTime.UtcNow);

        // Write the event to the outbox in the SAME transaction as the status change → it can't be lost.
        // Downstream: Inventory restocks, Auth emails the customer, Notification alerts admins.
        await uow.Outbox.AddAsync(OutboxMessage.Create(
            CartCancelledTopic, cart.Id.ToString(), JsonSerializer.Serialize(cancelledEvent), "CartCancelled", correlationId), ct);
        await uow.SaveChangesAsync(ct);

        await cacheService.RemoveCartAsync(cart.Id);

        logger.LogInformation("Order {CartId} cancelled. Reason: {Reason}", cart.Id, reason);
        return MapToResponse(cart);
    }

    public async Task<CartResponse> CheckoutAsync(CheckoutCartRequest request, string correlationId, CancellationToken ct = default)
    {
        var cart = await uow.Carts.GetByIdAsync(request.CartId, ct)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new BusinessException($"Cart {request.CartId} is not in Active status.");

        if (!cart.Items.Any())
            throw new BusinessException("Cannot checkout an empty cart.");

        // Mark the cart as CheckedOut in DB first so it can't be modified while the saga runs.
        cart.Checkout();

        // Snapshot item details into the event payload because cart items could change
        // (or the cart could be deleted) before consumers process this event.
        var snapshots = cart.Items
            .Select(i => new CartItemSnapshot(i.ProductId, i.Sku, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList()
            .AsReadOnly();

        // CartCheckedOutEvent kicks off the checkout saga (Inventory reserves stock) AND drives the
        // admin order notification + customer confirmation email (Notification service). Written to
        // the outbox in the SAME transaction as cart.Checkout() so the event is never lost — even if
        // Kafka is down right now, the OutboxRelay publishes it once Kafka is back.
        var checkedOutEvent = new CartCheckedOutEvent(
            cart.Id, cart.UserId, cart.TotalAmount, snapshots, correlationId, DateTime.UtcNow);
        await uow.Outbox.AddAsync(OutboxMessage.Create(
            CartCheckedOutTopic, cart.Id.ToString(), JsonSerializer.Serialize(checkedOutEvent), "CartCheckedOut", correlationId), ct);
        await uow.SaveChangesAsync(ct);

        // Cart is no longer active, so evict from cache to avoid serving stale data.
        await cacheService.RemoveCartAsync(cart.Id);

        logger.LogInformation("Cart {CartId} checked out. Saga initiated for user {UserId}", cart.Id, cart.UserId);
        return MapToResponse(cart);
    }

    // Saga compensation path: called by InventorySagaConsumer when inventory reservation fails.
    // Sets the cart to RolledBack so the user knows checkout did not complete.
    public async Task RollbackAsync(Guid cartId, string reason, CancellationToken ct = default)
    {
        var cart = await uow.Carts.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            // Guard against duplicate failure events arriving after a previous rollback deleted/archived the cart.
            logger.LogWarning("Rollback requested for unknown cart {CartId}", cartId);
            return;
        }

        cart.Rollback(reason);
        // Cart is already change-tracked; SaveChanges persists the change (no DbSet.Update needed).
        await uow.SaveChangesAsync(ct);

        await eventProducer.PublishCartRolledBackAsync(new CartRolledBackEvent(
            cart.Id, cart.UserId, reason, string.Empty, DateTime.UtcNow));

        await cacheService.RemoveCartAsync(cart.Id);

        logger.LogInformation("Cart {CartId} rolled back. Reason: {Reason}", cartId, reason);
    }

    private async Task<Cart> GetActiveCartAsync(Guid cartId, CancellationToken ct)
    {
        var cart = await uow.Carts.GetByIdAsync(cartId, ct)
            ?? throw new NotFoundException(nameof(Cart), cartId);

        if (cart.Status != CartStatus.Active)
            throw new BusinessException($"Cart {cartId} is not in Active status and cannot be modified.");

        return cart;
    }

    private static CartResponse MapToResponse(Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemResponse(
            i.Id,
            i.ProductId,
            i.Sku,
            i.ProductName,
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice,
            i.AddedAt));

        return new CartResponse(
            cart.Id,
            cart.UserId,
            cart.Status.ToString(),
            items,
            cart.TotalAmount,
            cart.ItemCount,
            cart.CreatedAt,
            cart.UpdatedAt);
    }
}
