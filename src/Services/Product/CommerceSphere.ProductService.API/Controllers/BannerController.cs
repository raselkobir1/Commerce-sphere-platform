using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.Shared.Common.Authorization;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.ProductService.API.Controllers;

[ApiController]
[Route("api/banners")]
[Produces("application/json")]
public class BannerController(IBannerManager bannerManager) : ControllerBase
{
    // Public read — the storefront carousel and the admin banners page both consume this.
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanners(CancellationToken ct)
    {
        var result = await bannerManager.GetAllAsync(ct);
        return Ok(ApiResponse<object>.Ok(result, "Banners retrieved", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPost]
    [HasPermission("banners:create")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBanner([FromBody] CreateBannerRequest request, CancellationToken ct)
    {
        var result = await bannerManager.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetBanners), null,
            ApiResponse<object>.Ok(result, "Banner created", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("banners:edit")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBanner(Guid id, [FromBody] UpdateBannerRequest request, CancellationToken ct)
    {
        var result = await bannerManager.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(result, "Banner updated", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("banners:delete")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBanner(Guid id, CancellationToken ct)
    {
        await bannerManager.DeleteAsync(id, ct);
        return Ok(ApiResponse.Ok("Banner deleted", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
