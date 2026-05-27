using FSH.Framework.Eventing.Abstractions;
using System.Text.Json;

namespace FSH.Framework.Eventing.Serialization;

/// <summary>
/// System.Text.Json-based event serializer.
/// </summary>
public sealed class JsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Serialize(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return JsonSerializer.Serialize(@event, @event.GetType(), Options);
    }

    public IIntegrationEvent? Deserialize(string payload, string eventTypeName)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(eventTypeName);

        var type = Type.GetType(eventTypeName, throwOnError: false);
        if (type is null)
        {
            return null;
        }

        var result = JsonSerializer.Deserialize(payload, type, Options);
        return result as IIntegrationEvent;
    }
}