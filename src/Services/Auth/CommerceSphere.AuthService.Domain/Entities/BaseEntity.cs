namespace CommerceSphere.AuthService.Domain.Entities;

// Base for all domain entities: handles identity, audit timestamps, and optimistic concurrency.
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    // EF Core uses RowVersion as an optimistic-concurrency token — a save will fail with
    // ConcurrencyException if another request updated the same row since we last read it.
    public uint RowVersion { get; protected set; }

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}
