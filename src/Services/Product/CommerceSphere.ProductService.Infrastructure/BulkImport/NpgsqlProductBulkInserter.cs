using System.Data;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace CommerceSphere.ProductService.Infrastructure.BulkImport;

// Persists products via PostgreSQL COPY (binary import) — the fastest insert path Npgsql offers,
// and far lighter than EF's change tracker at 100K rows. Column order/names must stay in lockstep
// with ProductConfiguration. The xmin concurrency column is system-generated, so it is omitted.
public class NpgsqlProductBulkInserter(ProductDbContext db) : IProductBulkInserter
{
    private const string CopyCommand =
        "COPY products (id, name, description, sku, price, category, image_url, " +
        "is_active, is_published, stock, created_at, updated_at) FROM STDIN (FORMAT BINARY)";

    public async Task<HashSet<string>> GetExistingSkusAsync(
        IReadOnlyCollection<string> skus, CancellationToken ct = default)
    {
        if (skus.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // EF translates Contains(list) to `sku = ANY(@skus)` — one round-trip for the whole batch.
        var found = await db.Products.AsNoTracking()
            .Where(p => skus.Contains(p.Sku))
            .Select(p => p.Sku)
            .ToListAsync(ct);

        return found.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task BulkInsertAsync(IReadOnlyCollection<Product> products, CancellationToken ct = default)
    {
        if (products.Count == 0)
            return;

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();

        var openedHere = false;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
            openedHere = true;
        }

        try
        {
            await using var writer = await connection.BeginBinaryImportAsync(CopyCommand, ct);

            foreach (var p in products)
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(p.Id, NpgsqlDbType.Uuid, ct);
                await writer.WriteAsync(p.Name, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(p.Description, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(p.Sku, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(p.Price, NpgsqlDbType.Numeric, ct);
                await writer.WriteAsync(p.Category, NpgsqlDbType.Text, ct);

                if (p.ImageUrl is null)
                    await writer.WriteNullAsync(ct);
                else
                    await writer.WriteAsync(p.ImageUrl, NpgsqlDbType.Text, ct);

                await writer.WriteAsync(p.IsActive, NpgsqlDbType.Boolean, ct);
                await writer.WriteAsync(p.IsPublished, NpgsqlDbType.Boolean, ct);
                await writer.WriteAsync(p.Stock, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(p.CreatedAt, NpgsqlDbType.TimestampTz, ct);

                if (p.UpdatedAt is null)
                    await writer.WriteNullAsync(ct);
                else
                    await writer.WriteAsync(p.UpdatedAt.Value, NpgsqlDbType.TimestampTz, ct);
            }

            await writer.CompleteAsync(ct);
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
