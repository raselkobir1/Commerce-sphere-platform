namespace CommerceSphere.ProductService.Infrastructure.Storage;

// Cloudinary credentials, bound from the "Cloudinary" config section (env: Cloudinary__CloudName, ...).
// Get these from your Cloudinary dashboard. Keep ApiSecret out of source control (.env.development).
public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    // Folder uploads are organised under, in your Cloudinary media library.
    public string Folder { get; set; } = "commerce-sphere";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ApiSecret);
}
