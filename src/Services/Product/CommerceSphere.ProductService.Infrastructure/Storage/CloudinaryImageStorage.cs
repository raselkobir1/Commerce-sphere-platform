using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommerceSphere.ProductService.Infrastructure.Storage;

// Uploads images to Cloudinary using a signed upload over their REST API (no SDK dependency).
// The API secret never leaves the server: we sign the request here, so the browser only ever talks
// to our upload endpoint. Returns the CDN secure_url to store in the product/banner imageUrl.
public class CloudinaryImageStorage(
    HttpClient http, IOptions<CloudinaryOptions> options, ILogger<CloudinaryImageStorage> logger) : IImageStorage
{
    private readonly CloudinaryOptions _opts = options.Value;

    public bool IsConfigured => _opts.IsConfigured;

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!_opts.IsConfigured)
            throw new BusinessException("Image upload is not configured. Set the Cloudinary credentials.");

        // Cloudinary signed upload: sign the alphabetically-sorted params (excluding file/api_key)
        // plus the api_secret. We sign folder + timestamp.
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var toSign = $"folder={_opts.Folder}&timestamp={timestamp}";
        var signature = Sha1Hex(toSign + _opts.ApiSecret);

        using var form = new MultipartFormDataContent
        {
            { CreateFileContent(content, fileName, contentType), "file", fileName },
            { new StringContent(_opts.ApiKey), "api_key" },
            { new StringContent(timestamp), "timestamp" },
            { new StringContent(_opts.Folder), "folder" },
            { new StringContent(signature), "signature" }
        };

        var endpoint = $"https://api.cloudinary.com/v1_1/{_opts.CloudName}/image/upload";
        using var response = await http.PostAsync(endpoint, form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Cloudinary upload failed. Status: {Status}, Body: {Body}", response.StatusCode, body);
            throw new BusinessException("Image upload failed. Please try again.");
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("secure_url", out var url) || url.GetString() is not { } secureUrl)
            throw new BusinessException("Image upload did not return a URL. Please try again.");

        logger.LogInformation("Image uploaded to Cloudinary: {Url}", secureUrl);
        return secureUrl;
    }

    private static StreamContent CreateFileContent(Stream content, string fileName, string contentType)
    {
        var file = new StreamContent(content);
        if (!string.IsNullOrWhiteSpace(contentType))
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return file;
    }

    private static string Sha1Hex(string input)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
