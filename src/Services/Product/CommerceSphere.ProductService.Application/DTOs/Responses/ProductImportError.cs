namespace CommerceSphere.ProductService.Application.DTOs.Responses;

// A single rejected row, written into the downloadable error-report workbook.
public sealed record ProductImportError(int RowNumber, string? Sku, string Reason);
