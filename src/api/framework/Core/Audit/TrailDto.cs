using System.Collections.ObjectModel;
using System.Text.Json;

namespace FSH.Framework.Core.Audit;
public class TrailDto()
{
    public Guid Id { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public Guid UserId { get; set; }
    public Dictionary<string, object?> KeyValues { get; } = [];
    public Dictionary<string, object?> OldValues { get; } = [];
    public Dictionary<string, object?> NewValues { get; } = [];
    public Collection<string> ModifiedProperties { get; } = [];
    public TrailType Type { get; set; }
    public string? TableName { get; set; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
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
            PrimaryKey = JsonSerializer.Serialize(KeyValues, SerializerOptions),
            PreviousValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues, SerializerOptions),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues, SerializerOptions),
            ModifiedProperties = ModifiedProperties.Count == 0 ? null : JsonSerializer.Serialize(ModifiedProperties, SerializerOptions)
        };
    }
}
