using FSH.Framework.Eventing.Abstractions;
using System.Collections.Concurrent;
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

    // Resolving an event type from its name repeats constantly on the outbox and inbox path, and the
    // underlying reflection lookup parses the assembly qualified name and scans loaded assemblies each
    // time, so the resolved result for each distinct name is cached here.
    private static readonly ConcurrentDictionary<string, Type?> TypeCache = new(StringComparer.Ordinal);

    public string Serialize(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return JsonSerializer.Serialize(@event, @event.GetType(), Options);
    }

    public IIntegrationEvent? Deserialize(string payload, string eventTypeName)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(eventTypeName);

        var type = TypeCache.GetOrAdd(eventTypeName, static n => Type.GetType(n, throwOnError: false));
        if (type is null)
        {
            return null;
        }

        var result = JsonSerializer.Deserialize(payload, type, Options);
        return result as IIntegrationEvent;
    }
}