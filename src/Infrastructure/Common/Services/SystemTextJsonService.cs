using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using FSH.WebApi.Application.Common.Interfaces;

namespace FSH.WebApi.Infrastructure.Common.Services;

public class SystemTextJsonService : ISerializerService
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonService()
    {
        _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public T Deserialize<T>(string text)
    {
        return JsonSerializer.Deserialize<T>(text, _options) ?? default!;
    }

    public string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public string Serialize<T>(T obj, Type type)
    {
        return JsonSerializer.Serialize(obj, type, _options);
    }
}