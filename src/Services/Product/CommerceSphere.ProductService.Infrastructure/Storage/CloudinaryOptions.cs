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

    // On-upload resize: images larger than these bounds are scaled down (aspect ratio kept, never
    // upscaled — c_limit), then quality-optimised. Keeps stored images small and fast to deliver.
    public int MaxWidth { get; set; } = 1600;
    public int MaxHeight { get; set; } = 1600;
    public string Quality { get; set; } = "auto:good";

    // The incoming-transformation string applied before the asset is stored (e.g. "c_limit,w_1600,h_1600,q_auto:good").
    public string Transformation => $"c_limit,w_{MaxWidth},h_{MaxHeight},q_{Quality}";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ApiSecret);
}
