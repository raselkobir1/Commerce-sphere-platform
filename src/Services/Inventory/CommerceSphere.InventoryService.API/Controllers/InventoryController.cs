using CommerceSphere.InventoryService.Application.DTOs.Requests;
using CommerceSphere.InventoryService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.InventoryService.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
public class InventoryController(IInventoryManager inventoryManager) : ControllerBase
{
    [HttpPost("reserve")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReserveInventory(
        [FromBody] ReserveInventoryRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKeyHeader,
        CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();

        // Prefer header value for idempotency key, fall back to request body value
        var effectiveRequest = idempotencyKeyHeader is not null
            ? request with { IdempotencyKey = idempotencyKeyHeader }
            : request;

        var result = await inventoryManager.ReserveAsync(effectiveRequest, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Inventory reserved successfully",
            HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPost("release")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseReservation(
        [FromBody] ReleaseReservationRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await inventoryManager.ReleaseAsync(request, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Reservation released successfully",
            HttpContext.TraceIdentifier, correlationId));
    }

    [HttpGet("product/{productId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductId(Guid productId, CancellationToken ct)
    {
        var result = await inventoryManager.GetByProductIdAsync(productId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Inventory item retrieved",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await inventoryManager.GetInventoryPagedAsync(pageNumber, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result, "Inventory retrieved",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustStock(
        [FromBody] AdjustStockRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await inventoryManager.AdjustStockAsync(request, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Stock adjusted successfully",
            HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPost("receive")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveStock(
        [FromBody] ReceiveStockRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await inventoryManager.ReceiveStockAsync(request, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Stock received successfully",
            HttpContext.TraceIdentifier, correlationId));
    }
}
