namespace CommerceSphere.ProductService.Domain.Entities;

// A promotional banner/slide shown in the storefront home-page carousel. Managed by admins.
public class Banner : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Subtitle { get; private set; } = string.Empty;
    public string ImageUrl { get; private set; } = string.Empty;
    public string LinkUrl { get; private set; } = string.Empty;  // optional click-through target
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }  // carousel order (ascending)

    private Banner() { }

    public static Banner Create(string title, string? subtitle, string imageUrl, string? linkUrl, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);
        return new Banner
        {
            Title = title.Trim(),
            Subtitle = subtitle?.Trim() ?? string.Empty,
            ImageUrl = imageUrl.Trim(),
            LinkUrl = linkUrl?.Trim() ?? string.Empty,
            IsActive = true,
            SortOrder = sortOrder,
        };
    }

    public void Update(string title, string? subtitle, string imageUrl, string? linkUrl, bool isActive, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);
        Title = title.Trim();
        Subtitle = subtitle?.Trim() ?? string.Empty;
        ImageUrl = imageUrl.Trim();
        LinkUrl = linkUrl?.Trim() ?? string.Empty;
        IsActive = isActive;
        SortOrder = sortOrder;
        SetUpdated();
    }
}
