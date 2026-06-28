using CommerceSphere.ProductService.Application.DTOs.Responses;

namespace CommerceSphere.ProductService.Application.Interfaces;

public interface IProductImportService
{
    // The blank workbook the admin downloads, fills, and re-uploads.
    byte[] GetTemplate();

    // Stores the upload, creates a Pending job, enqueues it, and returns immediately (202 flow).
    Task<BulkImportJobResponse> CreateJobAsync(
        Stream fileStream, string fileName, string? createdBy, string correlationId, CancellationToken ct = default);

    Task<BulkImportJobResponse?> GetJobAsync(Guid jobId, CancellationToken ct = default);

    // The downloadable error-report workbook, or null if the job had no rejected rows.
    Task<Stream?> GetErrorReportAsync(Guid jobId, CancellationToken ct = default);

    // Invoked by the background worker: streams, validates, dedupes and bulk-inserts the rows.
    Task ProcessJobAsync(Guid jobId, string correlationId, CancellationToken ct = default);
}
