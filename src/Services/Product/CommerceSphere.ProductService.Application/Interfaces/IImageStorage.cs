namespace CommerceSphere.ProductService.Application.Interfaces;

// Uploads an image to the configured store (Cloudinary) and returns its public URL.
// Abstracted here so managers/controllers stay independent of the storage provider.
public interface IImageStorage
{
    // True once the storage provider is configured (credentials present).
    bool IsConfigured { get; }

    // Uploads the image bytes and returns the hosted URL to persist in imageUrl.
    Task<string> UploadAsync(byte[] content, string fileName, string contentType, CancellationToken ct = default);
}
