using System.Net.Http.Headers;
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

    public async Task<string> UploadAsync(byte[] content, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!_opts.IsConfigured)
            throw new BusinessException("Image upload is not configured. Set the Cloudinary credentials.");

        // Cloudinary signed upload: sign the alphabetically-sorted params (excluding file/api_key)
        // plus the api_secret. We sign folder + timestamp + transformation (the on-upload resize).
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var transformation = _opts.Transformation;
        var toSign = $"folder={_opts.Folder}&timestamp={timestamp}&transformation={transformation}";
        var signature = Sha1Hex(toSign + _opts.ApiSecret);

        using var form = new MultipartFormDataContent();
        AddField(form, "api_key", _opts.ApiKey);
        AddField(form, "timestamp", timestamp);
        AddField(form, "folder", _opts.Folder);
        AddField(form, "transformation", transformation);   // resize + optimise before storing
        AddField(form, "signature", signature);
        AddFile(form, content, fileName, contentType);

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

        var deliveryUrl = ApplyDeliveryTransformation(secureUrl);
        logger.LogInformation("Image uploaded to Cloudinary: {Url}", deliveryUrl);
        return deliveryUrl;
    }

    // Bake the delivery transformation (f_auto,q_auto) into the URL so every consumer automatically
    // gets an optimally-formatted, quality-tuned image. Inserts it right after "/upload/".
    private string ApplyDeliveryTransformation(string secureUrl)
    {
        var t = _opts.DeliveryTransformation;
        const string marker = "/upload/";
        if (string.IsNullOrWhiteSpace(t) || !secureUrl.Contains(marker))
            return secureUrl;
        return secureUrl.Replace(marker, $"{marker}{t}/");
    }

    // Adds a text field. Content-Disposition names are set explicitly WITH QUOTES — .NET's default
    // MultipartFormDataContent emits unquoted names (name=api_key), which Cloudinary's strict
    // RFC 7578 parser ignores, causing it to treat a signed upload as unsigned. Quoting fixes that.
    private static void AddField(MultipartFormDataContent form, string name, string value)
    {
        var field = new StringContent(value);
        field.Headers.ContentType = null;   // plain form field, like `curl -F name=value`
        field.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = $"\"{name}\"" };
        form.Add(field);
    }

    private static void AddFile(MultipartFormDataContent form, byte[] content, string fileName, string contentType)
    {
        var file = new ByteArrayContent(content);
        if (!string.IsNullOrWhiteSpace(contentType))
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        file.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"file\"",
            FileName = $"\"{fileName}\""
        };
        form.Add(file);
    }

    private static string Sha1Hex(string input)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
