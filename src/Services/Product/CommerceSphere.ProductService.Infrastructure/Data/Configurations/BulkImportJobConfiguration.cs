using CommerceSphere.ProductService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.ProductService.Infrastructure.Data.Configurations;

public class BulkImportJobConfiguration : IEntityTypeConfiguration<BulkImportJob>
{
    public void Configure(EntityTypeBuilder<BulkImportJob> builder)
    {
        builder.ToTable("bulk_import_jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
        builder.Property(j => j.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(j => j.TotalRows).HasColumnName("total_rows");
        builder.Property(j => j.ProcessedRows).HasColumnName("processed_rows");
        builder.Property(j => j.SucceededRows).HasColumnName("succeeded_rows");
        builder.Property(j => j.FailedRows).HasColumnName("failed_rows");
        builder.Property(j => j.HasErrorReport).HasColumnName("has_error_report");
        builder.Property(j => j.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(j => j.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        builder.Property(j => j.CreatedAt).HasColumnName("created_at");
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at");
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at");

        // Same xmin concurrency-token mapping as ProductConfiguration (see the note there).
        builder.Property(j => j.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(j => j.CreatedAt).HasDatabaseName("ix_bulk_import_jobs_created_at");
    }
}
