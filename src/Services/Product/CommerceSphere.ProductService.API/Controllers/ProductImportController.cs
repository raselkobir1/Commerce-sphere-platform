using System.Security.Claims;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.ProductService.API.Controllers;

// Bulk product upload (admin). Flow:
//   1. GET  template  → download the .xlsx to fill in.
//   2. POST           → upload the filled workbook; returns 202 + jobId immediately.
//   3. GET  {jobId}   → poll job status / progress.
//   4. GET  {jobId}/errors → download the report of rejected rows (when any).
[ApiController]
[Route("api/products/import")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class ProductImportController(IProductImportService importService) : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    // Generous ceiling: a 100K-row workbook is typically 10-20 MB.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        var bytes = importService.GetTemplate();
        return File(bytes, XlsxContentType, "product-import-template.xlsx");
    }

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException(["An Excel (.xlsx) file is required."]);

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(["Only .xlsx files are supported. Download the template and fill it in."]);

        var correlationId = HttpContext.GetCorrelationId();
        var createdBy = User.FindFirstValue("sub") ?? User.Identity?.Name;

        await using var stream = file.OpenReadStream();
        var result = await importService.CreateJobAsync(stream, file.FileName, createdBy, correlationId, ct);

        // 202 Accepted — processing happens asynchronously; the client polls the status endpoint.
        return AcceptedAtAction(
            nameof(GetStatus),
            new { jobId = result.JobId },
            ApiResponse<object>.Ok(result, "Upload accepted. Processing has started.", HttpContext.TraceIdentifier, correlationId));
    }

    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct)
    {
        var result = await importService.GetJobAsync(jobId, ct)
            ?? throw new NotFoundException("BulkImportJob", jobId);

        return Ok(ApiResponse<object>.Ok(result, "Import job status", HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    [HttpGet("{jobId:guid}/errors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadErrorReport(Guid jobId, CancellationToken ct)
    {
        var report = await importService.GetErrorReportAsync(jobId, ct)
            ?? throw new NotFoundException("BulkImportErrorReport", jobId);

        return File(report, XlsxContentType, $"import-errors-{jobId}.xlsx");
    }
}
