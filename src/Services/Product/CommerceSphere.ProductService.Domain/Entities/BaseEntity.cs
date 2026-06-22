namespace CommerceSphere.ProductService.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public uint RowVersion { get; protected set; }

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}
