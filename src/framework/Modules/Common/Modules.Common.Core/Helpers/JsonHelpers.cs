using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSH.Framework.Core.Helpers;
public static class JsonHelpers
{
    public static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}