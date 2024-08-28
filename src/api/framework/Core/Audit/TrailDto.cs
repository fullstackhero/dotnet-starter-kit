using System.Collections.ObjectModel;
using System.Text.Json;

namespace FSH.Framework.Core.Audit;
public class TrailDto()
{
    public Guid Id { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public Guid UserId { get; set; }
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public Collection<string> ModifiedProperties { get; } = new();
    public TrailType Type { get; set; }
    public string? TableName { get; set; }

    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = false,
    };

    public AuditTrail ToAuditTrail()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Operation = Type.ToString(),
            Entity = TableName,
            DateTime = DateTime,
            PrimaryKey = JsonSerializer.Serialize(KeyValues, serializerOptions),
            PreviousValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues, serializerOptions),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues, serializerOptions),
            ModifiedProperties = ModifiedProperties.Count == 0 ? null : JsonSerializer.Serialize(ModifiedProperties, serializerOptions)
        };
    }
}
