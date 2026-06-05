using FSH.Modules.Auditing.Contracts;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FSH.Modules.Auditing;

/// <summary>
/// Simple masking by field-name convention or attributes.
/// </summary>
public sealed class JsonMaskingService : IAuditMaskingService
{
    private static readonly HashSet<string> MaskKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "token", "otp", "pin",
        "accessToken", "refreshToken", "apiKey", "clientSecret",
        "authCode", "authorization", "bearer", "connectionString"
    };

    private const string MaskValue = "****";

    public MaskingResult ApplyMasking(object payload)
    {
        try
        {
            var json = JsonSerializer.SerializeToNode(payload);
            if (json is null) return new MaskingResult(payload, 0);

            int maskedCount = 0;
            MaskNode(json, ref maskedCount);

            // No fields matched — return the original reference so callers skip the
            // AuditTag.PiiMasked tag and the extra serialization hop in the sink.
            return maskedCount == 0
                ? new MaskingResult(payload, 0)
                : new MaskingResult(json, maskedCount);
        }
        catch (JsonException)
        {
            return new MaskingResult(payload, 0); // safe fallback — payload is not valid JSON
        }
    }

    private static void MaskNode(JsonNode node, ref int maskedCount)
    {
        if (node is JsonObject obj)
        {
            foreach (var kvp in obj.ToList())
            {
                if (ShouldMask(kvp.Key))
                {
                    obj[kvp.Key] = MaskValue;
                    maskedCount++;
                }
                else if (kvp.Value is not null)
                {
                    MaskNode(kvp.Value, ref maskedCount);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var el in arr)
                if (el is not null) MaskNode(el, ref maskedCount);
        }
    }

    private static bool ShouldMask(string key)
        => MaskKeywords.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));
}
