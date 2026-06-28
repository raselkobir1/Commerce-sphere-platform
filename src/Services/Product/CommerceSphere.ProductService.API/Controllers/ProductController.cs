using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.Shared.Common.Authorization;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.ProductService.API.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductController(IProductManager productManager) : ControllerBase
{
    [HttpPost]
    [HasPermission("products:create")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await productManager.CreateAsync(request, correlationId, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id },
            ApiResponse<object>.Ok(result, "Product created successfully", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("products:edit")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await productManager.UpdateAsync(id, request, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Product updated successfully", HttpContext.TraceIdentifier, correlationId));
    }

    // Bulk publish / unpublish (admin product list). Only published products appear in the store.
    [HttpPost("publish")]
    [HasPermission("products:edit")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishProducts([FromBody] PublishProductsRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var count = await productManager.PublishProductsAsync(request, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(new { updated = count, request.Published },
            request.Published ? "Products published" : "Products unpublished", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var result = await productManager.GetByIdAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(result, "Product retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsRequest request, CancellationToken ct)
    {
        var result = await productManager.GetPagedAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(result, "Products retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission("products:edit")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProduct(Guid id, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await productManager.ActivateAsync(id, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Product activated", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission("products:edit")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await productManager.DeactivateAsync(id, correlationId, ct);
        return Ok(ApiResponse<object>.Ok(result, "Product deactivated", HttpContext.TraceIdentifier, correlationId));
    }
}
