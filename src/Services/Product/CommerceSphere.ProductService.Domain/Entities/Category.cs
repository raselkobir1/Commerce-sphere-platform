namespace CommerceSphere.ProductService.Domain.Entities;

// A managed catalog category. Products store the category name as a string, so this table is the
// authoritative list of selectable category names (used to populate the product form dropdown).
public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public Guid? ParentId { get; private set; }  // null = top-level; set = sub-category
    public int SortOrder { get; private set; }

    private Category() { }

    public static Category Create(string name, string? description, Guid? parentId = null, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            IsActive = true,
            ParentId = parentId,
            SortOrder = sortOrder,
        };
    }

    public void Update(string name, string? description, bool isActive, Guid? parentId, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
        ParentId = parentId;
        SortOrder = sortOrder;
        SetUpdated();
    }
}
