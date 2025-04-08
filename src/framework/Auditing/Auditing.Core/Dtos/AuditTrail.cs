using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FSH.Framework.Auditing.Core.Enums;

namespace FSH.Framework.Auditing.Core.Dtos;

public class AuditTrail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public AuditOperation Operation { get; set; } // e.g., "Create", "Update", "Delete"
    public string? EntityName { get; set; } // Name of the entity/table affected

    // Store dictionaries as JSON in the database
    [Column(TypeName = "jsonb")]
    public string KeyValuesJson { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string OldValuesJson { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string NewValuesJson { get; set; } = string.Empty;

    // Store ModifiedProperties as JSON
    [Column(TypeName = "jsonb")]
    public string ModifiedPropertiesJson { get; set; } = string.Empty;

    // Convert JSON back to Dictionary or List when needed
    public Dictionary<string, object?> KeyValues
    {
        get => JsonSerializer.Deserialize<Dictionary<string, object?>>(KeyValuesJson) ?? new Dictionary<string, object?>();
        set => KeyValuesJson = JsonSerializer.Serialize(value);
    }

    public Dictionary<string, object?> OldValues
    {
        get => JsonSerializer.Deserialize<Dictionary<string, object?>>(OldValuesJson) ?? new Dictionary<string, object?>();
        set => OldValuesJson = JsonSerializer.Serialize(value);
    }

    public Dictionary<string, object?> NewValues
    {
        get => JsonSerializer.Deserialize<Dictionary<string, object?>>(NewValuesJson) ?? new Dictionary<string, object?>();
        set => NewValuesJson = JsonSerializer.Serialize(value);
    }

    public Collection<string> ModifiedProperties
    {
        get => JsonSerializer.Deserialize<Collection<string>>(ModifiedPropertiesJson) ?? new Collection<string>();
        set => ModifiedPropertiesJson = JsonSerializer.Serialize(value);
    }
}
