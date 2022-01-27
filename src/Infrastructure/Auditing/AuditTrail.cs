using FSH.WebApi.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FSH.WebApi.Infrastructure.Auditing;

public class AuditTrail
{
    private readonly ISerializerService _serializer;

    public AuditTrail(EntityEntry entry, ISerializerService serializer)
    {
        Entry = entry;
        _serializer = serializer;
    }

    public EntityEntry Entry { get; }
    public Guid UserId { get; set; }
    public string? TableName { get; set; }
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();
    public TrailType TrailType { get; set; }
    public List<string> ChangedColumns { get; } = new();
    public bool HasTemporaryProperties => TemporaryProperties.Count > 0;

    public Trail ToAuditTrail() =>
        new()
        {
            UserId = UserId,
            Type = TrailType.ToString(),
            TableName = TableName,
            DateTime = DateTime.UtcNow,
            PrimaryKey = _serializer.Serialize(KeyValues),
            OldValues = OldValues.Count == 0 ? null : _serializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : _serializer.Serialize(NewValues),
            AffectedColumns = ChangedColumns.Count == 0 ? null : _serializer.Serialize(ChangedColumns)
        };
}