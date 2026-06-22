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
        uow.Carts.Update(cart);
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
        uow.Carts.Update(cart);
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
        uow.Carts.Update(cart);
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

    public async Task<CartResponse> CheckoutAsync(CheckoutCartRequest request, string correlationId, CancellationToken ct = default)
    {
        var cart = await uow.Carts.GetByIdAsync(request.CartId, ct)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new BusinessException($"Cart {request.CartId} is not in Active status.");

        if (!cart.Items.Any())
            throw new BusinessException("Cannot checkout an empty cart.");

        cart.Checkout();
        uow.Carts.Update(cart);
        await uow.SaveChangesAsync(ct);

        var snapshots = cart.Items
            .Select(i => new CartItemSnapshot(i.ProductId, i.Sku, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList()
            .AsReadOnly();

        await eventProducer.PublishCartCheckedOutAsync(new CartCheckedOutEvent(
            cart.Id, cart.UserId, cart.TotalAmount, snapshots, correlationId, DateTime.UtcNow));

        await cacheService.RemoveCartAsync(cart.Id);

        logger.LogInformation("Cart {CartId} checked out. Saga initiated for user {UserId}", cart.Id, cart.UserId);
        return MapToResponse(cart);
    }

    public async Task RollbackAsync(Guid cartId, string reason, CancellationToken ct = default)
    {
        var cart = await uow.Carts.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            logger.LogWarning("Rollback requested for unknown cart {CartId}", cartId);
            return;
        }

        cart.Rollback(reason);
        uow.Carts.Update(cart);
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
