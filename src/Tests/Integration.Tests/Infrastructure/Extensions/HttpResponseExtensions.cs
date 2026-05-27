using System.Text.Json;

namespace Integration.Tests.Infrastructure.Extensions;

public static class HttpResponseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T> DeserializeAsync<T>(this HttpResponseMessage response, CancellationToken ct = default)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}. Body: {json}");
    }

    public static async Task<T?> TryDeserializeAsync<T>(this HttpResponseMessage response, CancellationToken ct = default)
    {
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
