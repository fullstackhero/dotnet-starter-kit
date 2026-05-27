using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace FSH.Modules.Auditing;

internal static class HttpBodyReader
{
    public static async Task<(object? preview, int size)> ReadRequestAsync(HttpContext ctx, int maxBytes, CancellationToken ct)
    {
        if (ctx.Request.Body is null || ctx.Request.ContentLength == 0) return (null, 0);

        ctx.Request.EnableBuffering();
        using var ms = new MemoryStream();
        var copied = await CopyCappedAsync(ctx.Request.Body, ms, maxBytes, ct);
        ctx.Request.Body.Position = 0;

        return DeserializePreview(ms, copied);
    }

    public static async Task<(object? preview, int size)> ReadResponseAsync(Stream source, int maxBytes, CancellationToken ct)
    {
        // source is the response-body tee stream we control
        if (source.Length == 0) return (null, 0);
        source.Position = 0;

        using var ms = new MemoryStream();
        var copied = await CopyCappedAsync(source, ms, maxBytes, ct);
        return DeserializePreview(ms, copied);
    }

    private static async Task<int> CopyCappedAsync(Stream src, Stream dst, int maxBytes, CancellationToken ct)
    {
        var buf = new byte[8 * 1024];
        int total = 0, read;
        while ((read = await src.ReadAsync(buf, ct)) > 0)
        {
            var toWrite = Math.Min(read, Math.Max(0, maxBytes - total));
            if (toWrite > 0) await dst.WriteAsync(buf.AsMemory(0, toWrite), ct);
            total += read;
            if (total >= maxBytes) break;
        }
        return total;
    }

    private static (object? preview, int size) DeserializePreview(MemoryStream ms, int totalBytes)
    {
        try
        {
            ms.Position = 0;
            using var doc = JsonDocument.Parse(ms.ToArray());
            return (ToPlain(doc.RootElement), totalBytes);
        }
        catch (JsonException)
        {
            // Not valid JSON; return UTF8 snippet as fallback
            ms.Position = 0;
            var text = Encoding.UTF8.GetString(ms.ToArray());
            var snippet = text.Length > 2000 ? text[..2000] + ".(truncated)" : text;
            return (new { text = snippet }, totalBytes);
        }
    }

    private static object? ToPlain(JsonElement e)
    {
        return e.ValueKind switch
        {
            JsonValueKind.Object => e.EnumerateObject()
                .ToDictionary(p => p.Name, p => ToPlain(p.Value)),

            JsonValueKind.Array => e.EnumerateArray()
                .Select(ToPlain)
                .ToList(),

            JsonValueKind.String => e.GetString(),

            JsonValueKind.Number => GetNumericValue(e),

            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static object GetNumericValue(JsonElement e)
    {
        if (e.TryGetInt64(out var longValue))
            return longValue;

        if (e.TryGetDouble(out var doubleValue))
            return doubleValue;

        return e.GetRawText();
    }

}