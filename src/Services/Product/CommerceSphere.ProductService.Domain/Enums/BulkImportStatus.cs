namespace CommerceSphere.ProductService.Domain.Enums;

public enum BulkImportStatus
{
    // Job created and queued, not picked up by the worker yet.
    Pending = 0,

    // Worker is streaming, validating and inserting rows.
    Processing = 1,

    // Every row was inserted successfully.
    Completed = 2,

    // Finished, but some rows were rejected (duplicate SKU / validation). See the error report.
    CompletedWithErrors = 3,

    // The job aborted before finishing (bad file, unexpected error). See ErrorMessage.
    Failed = 4
}
