namespace CommerceSphere.ProductService.Application.DTOs.Responses;

public record BulkImportJobResponse(
    Guid JobId,
    string FileName,
    string Status,
    int TotalRows,
    int ProcessedRows,
    int SucceededRows,
    int FailedRows,
    bool HasErrorReport,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
