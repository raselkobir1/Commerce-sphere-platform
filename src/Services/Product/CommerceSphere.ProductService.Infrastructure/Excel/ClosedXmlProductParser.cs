using ClosedXML.Excel;
using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;
using CommerceSphere.ProductService.Application.Interfaces;

namespace CommerceSphere.ProductService.Infrastructure.Excel;

// ClosedXML-backed (MIT-licensed) reader/writer for the bulk-import workbooks.
//
// Scaling note: ClosedXML loads the workbook DOM into memory, so peak RAM scales with file size
// (~150-300 MB for a 100K-row sheet). That is acceptable for an infrequent admin background job.
// Parse() still yields rows lazily so the *importer's* own allocations stay bounded to one batch.
// For files an order of magnitude larger, swap this for a DocumentFormat.OpenXml SAX reader.
public class ClosedXmlProductParser : IExcelProductParser
{
    private const string DataSheetName = "Products";

    // Canonical column keys → accepted header spellings (compared case-insensitively, spaces stripped).
    private static readonly string[] Headers =
        ["Name", "Description", "Sku", "Price", "Category", "ImageUrl", "InitialStock"];

    private static readonly string[] RequiredHeaders = ["Name", "Sku", "Price", "Category", "InitialStock"];

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add(DataSheetName);
        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // One illustrative row so the admin sees the expected shape.
        sheet.Cell(2, 1).Value = "Wireless Mouse";
        sheet.Cell(2, 2).Value = "2.4GHz ergonomic wireless mouse";
        sheet.Cell(2, 3).Value = "WM-001";
        sheet.Cell(2, 4).Value = 19.99;
        sheet.Cell(2, 5).Value = "Accessories";
        sheet.Cell(2, 6).Value = "https://cdn.example.com/wm-001.jpg";
        sheet.Cell(2, 7).Value = 150;

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        var help = workbook.Worksheets.Add("Instructions");
        var lines = new[]
        {
            "CommerceSphere — Bulk Product Upload",
            "",
            "1. Fill in the 'Products' sheet, one product per row, below the header row.",
            "2. Do NOT rename, reorder or remove the header columns.",
            "3. Required columns: Name, Sku, Price, Category, InitialStock.",
            "4. Optional columns: Description, ImageUrl.",
            "",
            "Field rules:",
            "  - Name: required, max 200 characters.",
            "  - Description: optional, max 2000 characters.",
            "  - Sku: required, max 100 characters, letters/digits/hyphen/underscore only, unique.",
            "  - Price: required, number >= 0.",
            "  - Category: required, max 100 characters.",
            "  - ImageUrl: optional, must be a full absolute URL, max 500 characters.",
            "  - InitialStock: required, whole number >= 0.",
            "",
            "Notes:",
            "  - Duplicate SKUs (within the file or already in the catalog) are skipped and listed in the error report.",
            "  - Uploaded products start as DRAFTS — publish them from the admin product list once reviewed.",
        };
        for (var i = 0; i < lines.Length; i++)
            help.Cell(i + 1, 1).Value = lines[i];
        help.Column(1).Width = 90;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public IEnumerable<ProductImportRow> Parse(Stream workbook)
    {
        using var wb = new XLWorkbook(workbook);
        var sheet = wb.Worksheets.FirstOrDefault(w => w.Name.Equals(DataSheetName, StringComparison.OrdinalIgnoreCase))
                    ?? wb.Worksheets.First();

        var headerRow = sheet.Row(1);
        var columns = MapColumns(headerRow);

        foreach (var key in RequiredHeaders)
        {
            if (!columns.ContainsKey(key))
                throw new InvalidOperationException(
                    $"The uploaded file is missing the required '{key}' column. Download a fresh template and try again.");
        }

        // RowsUsed() skips trailing blank rows; ClosedXML yields lazily over the loaded sheet.
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            // A row where every mapped cell is blank is treated as the end of data, not an error.
            if (columns.Values.All(col => row.Cell(col).IsEmpty()))
                continue;

            string? parseError = null;

            var price = ReadDecimal(row, columns, "Price", ref parseError);
            var stock = ReadInt(row, columns, "InitialStock", ref parseError);

            yield return new ProductImportRow
            {
                RowNumber = row.RowNumber(),
                Name = ReadString(row, columns, "Name"),
                Description = ReadString(row, columns, "Description"),
                Sku = ReadString(row, columns, "Sku"),
                Price = price,
                Category = ReadString(row, columns, "Category"),
                ImageUrl = ReadString(row, columns, "ImageUrl"),
                InitialStock = stock,
                ParseError = parseError
            };
        }
    }

    public byte[] BuildErrorReport(IEnumerable<ProductImportError> errors)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Errors");

        string[] headers = ["Row", "Sku", "Reason"];
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var r = 2;
        foreach (var e in errors)
        {
            sheet.Cell(r, 1).Value = e.RowNumber;
            sheet.Cell(r, 2).Value = e.Sku ?? string.Empty;
            sheet.Cell(r, 3).Value = e.Reason;
            r++;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static Dictionary<string, int> MapColumns(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in headerRow.CellsUsed())
        {
            var normalized = Normalize(cell.GetString());
            var key = Headers.FirstOrDefault(h => Normalize(h) == normalized);
            if (key is not null && !map.ContainsKey(key))
                map[key] = cell.Address.ColumnNumber;
        }
        return map;
    }

    private static string Normalize(string header) =>
        header.Replace(" ", string.Empty).Replace("_", string.Empty).Trim().ToLowerInvariant();

    private static string? ReadString(IXLRow row, IReadOnlyDictionary<string, int> columns, string key)
    {
        if (!columns.TryGetValue(key, out var col))
            return null;
        var value = row.Cell(col).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static decimal ReadDecimal(IXLRow row, IReadOnlyDictionary<string, int> columns, string key, ref string? parseError)
    {
        if (!columns.TryGetValue(key, out var col))
            return 0m;

        var cell = row.Cell(col);
        if (cell.IsEmpty())
            return 0m;

        if (cell.TryGetValue<decimal>(out var value))
            return value;

        parseError ??= $"{key} '{cell.GetString()}' is not a valid number.";
        return 0m;
    }

    private static int ReadInt(IXLRow row, IReadOnlyDictionary<string, int> columns, string key, ref string? parseError)
    {
        if (!columns.TryGetValue(key, out var col))
            return 0;

        var cell = row.Cell(col);
        if (cell.IsEmpty())
            return 0;

        if (cell.TryGetValue<int>(out var value))
            return value;

        parseError ??= $"{key} '{cell.GetString()}' is not a whole number.";
        return 0;
    }
}
