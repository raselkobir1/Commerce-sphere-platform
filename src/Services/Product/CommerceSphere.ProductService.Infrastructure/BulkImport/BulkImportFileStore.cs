using CommerceSphere.ProductService.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CommerceSphere.ProductService.Infrastructure.BulkImport;

// Stores uploaded workbooks and generated error reports on local disk, keyed by job id, so they
// stay out of the database and survive past the HTTP request that created them. The base path is
// configurable via BulkImport:StoragePath (defaults to a temp subfolder).
public class BulkImportFileStore : IBulkImportFileStore
{
    private readonly string _basePath;

    public BulkImportFileStore(IConfiguration config)
    {
        _basePath = config["BulkImport:StoragePath"]
            ?? Path.Combine(Path.GetTempPath(), "commercesphere-bulk-imports");
        Directory.CreateDirectory(_basePath);
    }

    private string UploadPath(Guid jobId) => Path.Combine(_basePath, $"{jobId}.xlsx");
    private string ErrorReportPath(Guid jobId) => Path.Combine(_basePath, $"{jobId}-errors.xlsx");

    public async Task SaveUploadAsync(Guid jobId, Stream content, CancellationToken ct = default)
    {
        await using var file = new FileStream(
            UploadPath(jobId), FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
    }

    public Task<Stream> OpenUploadAsync(Guid jobId, CancellationToken ct = default)
    {
        Stream stream = new FileStream(
            UploadPath(jobId), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public async Task SaveErrorReportAsync(Guid jobId, byte[] content, CancellationToken ct = default)
    {
        await File.WriteAllBytesAsync(ErrorReportPath(jobId), content, ct);
    }

    public Task<Stream?> OpenErrorReportAsync(Guid jobId, CancellationToken ct = default)
    {
        var path = ErrorReportPath(jobId);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }
}
