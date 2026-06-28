using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;

namespace CommerceSphere.ProductService.Application.Interfaces;

// Reads and writes the bulk-import workbooks. Implemented over a spreadsheet library in
// Infrastructure; the Application layer stays library-agnostic.
public interface IExcelProductParser
{
    // The blank template (with header + instructions) the admin downloads and fills in.
    byte[] GenerateTemplate();

    // Streams data rows lazily so a 100K-row workbook is never fully materialised in memory.
    IEnumerable<ProductImportRow> Parse(Stream workbook);

    // Builds the downloadable report listing every rejected row and why.
    byte[] BuildErrorReport(IEnumerable<ProductImportError> errors);
}
