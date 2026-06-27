namespace CommerceSphere.ProductService.Domain.Entities;

// A managed catalog category. Products store the category name as a string, so this table is the
// authoritative list of selectable category names (used to populate the product form dropdown).
public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Category() { }

    public static Category Create(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            IsActive = true,
        };
    }

    public void Update(string name, string? description, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
        SetUpdated();
    }
}
