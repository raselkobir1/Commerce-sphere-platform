namespace CommerceSphere.ProductService.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int Stock { get; private set; }

    private Product() { }

    public static Product Create(
        string name,
        string description,
        string sku,
        decimal price,
        string category,
        string? imageUrl,
        int initialStock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        if (price < 0)
            throw new ArgumentException("Price must be non-negative.", nameof(price));

        if (initialStock < 0)
            throw new ArgumentException("Initial stock must be non-negative.", nameof(initialStock));

        return new Product
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Sku = sku.Trim().ToUpperInvariant(),
            Price = price,
            Category = category.Trim(),
            ImageUrl = imageUrl?.Trim(),
            Stock = initialStock,
            IsActive = true
        };
    }

    public void Update(string name, string description, decimal price, string category, string? imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        if (price < 0)
            throw new ArgumentException("Price must be non-negative.", nameof(price));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        Category = category.Trim();
        ImageUrl = imageUrl?.Trim();
        SetUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }

    public void UpdateStock(int stock)
    {
        if (stock < 0)
            throw new ArgumentException("Stock must be non-negative.", nameof(stock));

        Stock = stock;
        SetUpdated();
    }
}
