using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.ProductService.API.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoryController(ICategoryManager categoryManager) : ControllerBase
{
    // Public read — the storefront and the admin product form both need the category list.
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await categoryManager.GetAllAsync(ct);
        return Ok(ApiResponse<object>.Ok(result, "Categories retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await categoryManager.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetCategories), null,
            ApiResponse<object>.Ok(result, "Category created", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await categoryManager.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(result, "Category updated", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        await categoryManager.DeleteAsync(id, ct);
        return Ok(ApiResponse.Ok("Category deleted", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
