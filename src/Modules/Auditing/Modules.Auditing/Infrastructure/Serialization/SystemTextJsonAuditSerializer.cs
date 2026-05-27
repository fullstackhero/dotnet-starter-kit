using FSH.Modules.Auditing.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSH.Modules.Auditing;

public sealed class SystemTextJsonAuditSerializer : IAuditSerializer
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    public string SerializePayload(object payload) => JsonSerializer.Serialize(payload, Opts);
}