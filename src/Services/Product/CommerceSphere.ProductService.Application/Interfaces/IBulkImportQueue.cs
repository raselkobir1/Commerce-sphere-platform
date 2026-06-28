namespace CommerceSphere.ProductService.Application.Interfaces;

// In-process hand-off from the upload endpoint to the background worker. Backed by an unbounded
// Channel; a single registered job id is enqueued after the workbook is safely stored.
public interface IBulkImportQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default);
    ValueTask<Guid> DequeueAsync(CancellationToken ct = default);
}
