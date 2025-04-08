using System.Text.Json;
using FSH.Framework.Auditing.Dtos;
using FSH.Framework.Auditing.Models;

namespace FSH.Framework.Auditing.Mappings;

public static class TrailMapping
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
    };

    public static AuditTrail ToAuditTrail(this TrailDto trail)
    {
        return new AuditTrail
        {
            Id = Guid.NewGuid(),
            UserId = trail.UserId,
            Operation = trail.Type.ToString(),
            Entity = trail.TableName,
            DateTime = trail.DateTime,
            PrimaryKey = JsonSerializer.Serialize(trail.KeyValues, SerializerOptions),
            PreviousValues = trail.OldValues.Count == 0 ? null : JsonSerializer.Serialize(trail.OldValues, SerializerOptions),
            NewValues = trail.NewValues.Count == 0 ? null : JsonSerializer.Serialize(trail.NewValues, SerializerOptions),
            ModifiedProperties = trail.ModifiedProperties.Count == 0 ? null : JsonSerializer.Serialize(trail.ModifiedProperties, SerializerOptions)
        };
    }
}
