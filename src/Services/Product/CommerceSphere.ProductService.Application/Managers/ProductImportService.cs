using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces;
using CommerceSphere.Shared.Contracts.Events.Product;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.ProductService.Application.Managers;

public class ProductImportService(
    IUnitOfWork uow,
    IExcelProductParser parser,
    IProductBulkInserter inserter,
    IBulkImportFileStore fileStore,
    IBulkImportQueue queue,
    IProductEventProducer eventProducer,
    IValidator<ProductImportRow> rowValidator,
    ILogger<ProductImportService> logger) : IProductImportService
{
    // Rows are validated, deduped and COPY-inserted one chunk at a time so memory stays flat
    // regardless of file size. 5K balances per-batch round-trips against DB statement size.
    private const int BatchSize = 5_000;

    public byte[] GetTemplate() => parser.GenerateTemplate();

    public async Task<BulkImportJobResponse> CreateJobAsync(
        Stream fileStream, string fileName, string? createdBy, string correlationId, CancellationToken ct = default)
    {
        var job = BulkImportJob.Create(fileName, createdBy);

        // Persist the workbook BEFORE the job row + enqueue, so the worker can never dequeue a
        // job whose file isn't there yet.
        await fileStore.SaveUploadAsync(job.Id, fileStream, ct);

        await uow.BulkImportJobs.AddAsync(job, ct);
        await uow.SaveChangesAsync(ct);

        await queue.EnqueueAsync(job.Id, ct);

        logger.LogInformation(
            "Bulk import job created. JobId: {JobId}, File: {FileName}, CorrelationId: {CorrelationId}",
            job.Id, job.FileName, correlationId);

        return MapToResponse(job);
    }

    public async Task<BulkImportJobResponse?> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await uow.BulkImportJobs.GetByIdAsync(jobId, ct);
        return job is null ? null : MapToResponse(job);
    }

    public async Task<Stream?> GetErrorReportAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await uow.BulkImportJobs.GetByIdAsync(jobId, ct);
        if (job is null || !job.HasErrorReport)
            return null;

        return await fileStore.OpenErrorReportAsync(jobId, ct);
    }

    public async Task ProcessJobAsync(Guid jobId, string correlationId, CancellationToken ct = default)
    {
        var job = await uow.BulkImportJobs.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            logger.LogWarning("Bulk import job {JobId} not found; nothing to process.", jobId);
            return;
        }

        // Guard against a duplicate enqueue re-processing a finished job.
        if (job.Status is not Domain.Enums.BulkImportStatus.Pending)
        {
            logger.LogInformation("Bulk import job {JobId} is {Status}; skipping.", jobId, job.Status);
            return;
        }

        job.Start();
        uow.BulkImportJobs.Update(job);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Bulk import job {JobId} started. CorrelationId: {CorrelationId}", jobId, correlationId);

        var errors = new List<ProductImportError>();
        // SKUs seen earlier in THIS file — a second occurrence is a duplicate even if the DB is clean.
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await using var upload = await fileStore.OpenUploadAsync(jobId, ct);

            foreach (var batch in Chunk(parser.Parse(upload), BatchSize))
            {
                ct.ThrowIfCancellationRequested();
                var (inserted, batchErrors) = await ProcessBatchAsync(batch, seenInFile, correlationId, ct);

                errors.AddRange(batchErrors);
                job.RecordBatch(inserted, batchErrors.Count);
                uow.BulkImportJobs.Update(job);
                await uow.SaveChangesAsync(ct);
            }

            job.SetTotalRows(job.ProcessedRows);

            if (errors.Count > 0)
            {
                var report = parser.BuildErrorReport(errors);
                await fileStore.SaveErrorReportAsync(jobId, report, ct);
                job.Complete(hasErrorReport: true);
            }
            else
            {
                job.Complete(hasErrorReport: false);
            }

            uow.BulkImportJobs.Update(job);
            await uow.SaveChangesAsync(ct);

            logger.LogInformation(
                "Bulk import job {JobId} finished. Succeeded: {Succeeded}, Failed: {Failed}",
                jobId, job.SucceededRows, job.FailedRows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bulk import job {JobId} failed.", jobId);
            job.Fail(ex.Message);
            uow.BulkImportJobs.Update(job);
            await uow.SaveChangesAsync(CancellationToken.None);
        }
    }

    // Validates + dedupes a chunk, COPY-inserts the survivors, then emits one ProductCreatedEvent
    // per inserted product (batched) so the Inventory service builds the matching stock records.
    private async Task<(int Inserted, List<ProductImportError> Errors)> ProcessBatchAsync(
        IReadOnlyList<ProductImportRow> rows, HashSet<string> seenInFile, string correlationId, CancellationToken ct)
    {
        var errors = new List<ProductImportError>();
        var candidates = new List<ProductImportRow>(rows.Count);

        foreach (var row in rows)
        {
            var validation = rowValidator.Validate(row);
            if (!validation.IsValid)
            {
                errors.Add(new ProductImportError(
                    row.RowNumber, row.Sku, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
                continue;
            }

            var normalizedSku = row.Sku!.Trim().ToUpperInvariant();
            if (!seenInFile.Add(normalizedSku))
            {
                errors.Add(new ProductImportError(row.RowNumber, row.Sku, "Duplicate SKU within the uploaded file."));
                continue;
            }

            candidates.Add(row);
        }

        if (candidates.Count == 0)
            return (0, errors);

        // Skip-and-report: any SKU already in the catalog is left untouched.
        var skus = candidates.Select(r => r.Sku!.Trim().ToUpperInvariant()).ToList();
        var existing = await inserter.GetExistingSkusAsync(skus, ct);

        var products = new List<Product>(candidates.Count);
        foreach (var row in candidates)
        {
            var normalizedSku = row.Sku!.Trim().ToUpperInvariant();
            if (existing.Contains(normalizedSku))
            {
                errors.Add(new ProductImportError(row.RowNumber, row.Sku, "A product with this SKU already exists."));
                continue;
            }

            // Imported products start as drafts (IsPublished = false) — the admin reviews and
            // bulk-publishes via POST /api/products/publish, matching single-create behaviour.
            products.Add(Product.Create(
                row.Name!, row.Description ?? string.Empty, row.Sku!, row.Price, row.Category!, row.ImageUrl, row.InitialStock));
        }

        if (products.Count == 0)
            return (0, errors);

        await inserter.BulkInsertAsync(products, ct);

        // NOTE: insert and event publish are not a single atomic unit (the Product service has no
        // outbox). A crash between the two would leave Inventory missing these SKUs; the
        // ProductCreatedConsumer is idempotent, so a manual re-publish is the recovery path.
        var events = products.Select(p => new ProductCreatedEvent(
            ProductId: p.Id,
            Name: p.Name,
            Sku: p.Sku,
            Price: p.Price,
            InitialStock: p.Stock,
            CorrelationId: correlationId,
            OccurredAt: DateTime.UtcNow));

        await eventProducer.PublishProductCreatedBatchAsync(events, ct);

        return (products.Count, errors);
    }

    // Buffers a lazy row sequence into fixed-size lists without materialising the whole file.
    private static IEnumerable<IReadOnlyList<ProductImportRow>> Chunk(IEnumerable<ProductImportRow> source, int size)
    {
        var bucket = new List<ProductImportRow>(size);
        foreach (var item in source)
        {
            bucket.Add(item);
            if (bucket.Count == size)
            {
                yield return bucket;
                bucket = new List<ProductImportRow>(size);
            }
        }

        if (bucket.Count > 0)
            yield return bucket;
    }

    private static BulkImportJobResponse MapToResponse(BulkImportJob j) =>
        new(j.Id, j.FileName, j.Status.ToString(), j.TotalRows, j.ProcessedRows,
            j.SucceededRows, j.FailedRows, j.HasErrorReport, j.ErrorMessage, j.CreatedAt, j.CompletedAt);
}
