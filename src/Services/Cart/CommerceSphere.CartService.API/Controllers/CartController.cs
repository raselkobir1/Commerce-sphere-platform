using CommerceSphere.CartService.Application.DTOs.Requests;
using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.CartService.API.Controllers;

[ApiController]
[Route("api/carts")]
[Produces("application/json")]
[Authorize]
public class CartController(ICartManager cartManager) : ControllerBase
{
    // Admin order list — all checked-out carts (who bought what, when). Placed before the
    // "{cartId:guid}" route; "orders" isn't a GUID so there's no conflict.
    [HttpGet("orders")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var result = await cartManager.GetOrdersAsync(ct);
        return Ok(ApiResponse<object>.Ok(result, "Orders retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCart([FromBody] CreateCartRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var idempotencyKey = HttpContext.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        var effectiveRequest = request with { IdempotencyKey = idempotencyKey ?? request.IdempotencyKey };

        var result = await cartManager.CreateCartAsync(effectiveRequest, correlationId, ct);
        return CreatedAtAction(nameof(GetCart), new { cartId = result.Id },
            ApiResponse<object>.Ok(result, "Cart created successfully", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPost("{cartId:guid}/items")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(Guid cartId, [FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await cartManager.AddItemAsync(cartId, request, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Item added to cart", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPut("{cartId:guid}/items")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(Guid cartId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var result = await cartManager.UpdateItemAsync(cartId, request, ct);
        return Ok(ApiResponse<object>.Ok(result, "Cart item updated", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpDelete("{cartId:guid}/items/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid cartId, Guid productId, CancellationToken ct)
    {
        var result = await cartManager.RemoveItemAsync(cartId, productId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Item removed from cart", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("{cartId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCart(Guid cartId, CancellationToken ct)
    {
        var result = await cartManager.GetCartAsync(cartId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Cart retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCartByUser(Guid userId, CancellationToken ct)
    {
        var result = await cartManager.GetCartByUserIdAsync(userId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Cart retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("{cartId:guid}/checkout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Checkout(Guid cartId, [FromBody] CheckoutCartRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var effectiveRequest = request with { CartId = cartId };
        var result = await cartManager.CheckoutAsync(effectiveRequest, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Cart checked out. Saga initiated.", HttpContext.TraceIdentifier, correlationId));
    }
}
