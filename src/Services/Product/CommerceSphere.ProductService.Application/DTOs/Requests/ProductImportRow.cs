namespace CommerceSphere.ProductService.Application.DTOs.Requests;

// One parsed row from the uploaded workbook. RowNumber is the 1-based sheet row (including the
// header) so validation errors can point the admin at the exact line to fix.
public sealed class ProductImportRow
{
    public int RowNumber { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal Price { get; init; }
    public string? Category { get; init; }
    public string? ImageUrl { get; init; }
    public int InitialStock { get; init; }

    // Set by the parser when a cell could not be converted (e.g. a non-numeric Price). Surfaced
    // verbatim by the validator so the admin sees exactly which cell to fix.
    public string? ParseError { get; init; }
}
