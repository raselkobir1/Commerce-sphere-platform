using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OtpNet;

namespace CommerceSphere.AuthService.IntegrationTests.Infrastructure;

// Minimal envelope reader: the API always responds with the ApiResponse<T> shape, so we parse
// success/message and hand back the `data` element for tests to navigate.
public sealed record ApiResult(System.Net.HttpStatusCode Status, bool Success, string Message, JsonElement Data)
{
    public bool IsSuccess => (int)Status is >= 200 and < 300;
}

public static class TestHelpers
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<ApiResult> ReadApiResultAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        // Auth-pipeline rejections (401 challenge, 403) short-circuit with an empty body — there is
        // no ApiResponse envelope to parse, so report the status with empty success/message.
        if (string.IsNullOrWhiteSpace(body))
            return new ApiResult(response.StatusCode, false, "", default);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
        var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        var data = root.TryGetProperty("data", out var d) ? d.Clone() : default;
        return new ApiResult(response.StatusCode, success, message, data);
    }

    public static async Task<ApiResult> PostJsonAsync(this HttpClient client, string url, object body)
        => await (await client.PostAsJsonAsync(url, body)).ReadApiResultAsync();

    public static void SetBearer(this HttpClient client, string accessToken)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public static void ClearBearer(this HttpClient client)
        => client.DefaultRequestHeaders.Authorization = null;

    public static string GetString(this JsonElement data, string prop) => data.GetProperty(prop).GetString()!;
    public static bool GetBool(this JsonElement data, string prop) => data.GetProperty(prop).GetBoolean();

    // Computes a valid TOTP code from a base32 secret — same algorithm the server validates with.
    public static string ComputeTotp(string base32Secret)
        => new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp();

    // A password that satisfies RegisterRequestValidator (upper, lower, digit, special, 8+).
    public static string ValidPassword => "Passw0rd!";

    public static string UniqueEmail(string prefix = "user")
        => $"{prefix}_{Guid.NewGuid():N}@test.dev";
}
