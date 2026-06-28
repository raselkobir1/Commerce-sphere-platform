namespace CommerceSphere.ProductService.Application.Interfaces;

// Persists the uploaded workbook and the generated error report out-of-band (keyed by job id),
// so neither bloats the database and the background worker can re-open the upload after the
// HTTP request has completed.
public interface IBulkImportFileStore
{
    Task SaveUploadAsync(Guid jobId, Stream content, CancellationToken ct = default);
    Task<Stream> OpenUploadAsync(Guid jobId, CancellationToken ct = default);

    Task SaveErrorReportAsync(Guid jobId, byte[] content, CancellationToken ct = default);
    Task<Stream?> OpenErrorReportAsync(Guid jobId, CancellationToken ct = default);
}
