using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.ProductService.API.Controllers;

// Image upload for the admin catalog (products + banners). Admins send a file; we forward it to
// Cloudinary (signed server-side) and return the hosted URL to save in the item's imageUrl.
[ApiController]
[Route("api/products/images")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class ImageController(IImageStorage imageStorage) : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;   // 5 MB
    private static readonly string[] AllowedTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    [HttpPost]
    [RequestSizeLimit(MaxBytes + 1024)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new BusinessException("No image file was provided.");
        if (file.Length > MaxBytes)
            throw new BusinessException("Image is too large (max 5 MB).");
        if (!AllowedTypes.Contains(file.ContentType))
            throw new BusinessException("Unsupported image type. Use JPEG, PNG, WebP, or GIF.");

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var url = await imageStorage.UploadAsync(buffer.ToArray(), file.FileName, file.ContentType, ct);

        return Ok(ApiResponse<object>.Ok(new { url }, "Image uploaded",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}
