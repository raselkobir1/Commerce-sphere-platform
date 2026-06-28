using CommerceSphere.ProductService.Domain.Enums;

namespace CommerceSphere.ProductService.Domain.Entities;

// Tracks one bulk product upload from creation through completion so the admin can poll
// progress and download a report of any rejected rows. The uploaded workbook and the error
// report are stored out-of-band (keyed by this job's Id) by IBulkImportFileStore.
public class BulkImportJob : BaseEntity
{
    public string FileName { get; private set; } = string.Empty;
    public BulkImportStatus Status { get; private set; } = BulkImportStatus.Pending;

    // 0 until the worker has finished a first pass over the sheet and knows the row count.
    public int TotalRows { get; private set; }
    public int ProcessedRows { get; private set; }
    public int SucceededRows { get; private set; }
    public int FailedRows { get; private set; }

    // Set only when at least one row was rejected; the report workbook is stored under this key.
    public bool HasErrorReport { get; private set; }

    // Populated only when Status == Failed.
    public string? ErrorMessage { get; private set; }

    public string? CreatedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private BulkImportJob() { }

    public static BulkImportJob Create(string fileName, string? createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return new BulkImportJob
        {
            FileName = fileName.Trim(),
            CreatedBy = createdBy,
            Status = BulkImportStatus.Pending
        };
    }

    public void Start()
    {
        Status = BulkImportStatus.Processing;
        SetUpdated();
    }

    public void SetTotalRows(int totalRows)
    {
        if (totalRows < 0)
            throw new ArgumentException("Total rows must be non-negative.", nameof(totalRows));

        TotalRows = totalRows;
        SetUpdated();
    }

    // Called after every processed chunk so polling reflects live progress.
    public void RecordBatch(int succeeded, int failed)
    {
        if (succeeded < 0 || failed < 0)
            throw new ArgumentException("Batch counts must be non-negative.");

        SucceededRows += succeeded;
        FailedRows += failed;
        ProcessedRows += succeeded + failed;
        SetUpdated();
    }

    public void Complete(bool hasErrorReport)
    {
        HasErrorReport = hasErrorReport;
        Status = FailedRows > 0
            ? BulkImportStatus.CompletedWithErrors
            : BulkImportStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Fail(string errorMessage)
    {
        Status = BulkImportStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        SetUpdated();
    }
}
