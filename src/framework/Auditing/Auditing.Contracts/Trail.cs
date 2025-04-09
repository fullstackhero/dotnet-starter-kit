using FSH.Framework.Auditing.Contracts.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FSH.Framework.Auditing.Contracts;

public class Trail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public AuditOperation Operation { get; set; } // e.g., "Create", "Update", "Delete"
    public required string Description { get; set; }
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

    [NotMapped]
    public IReadOnlyDictionary<string, object?> OldValues =>
     JsonSerializer.Deserialize<Dictionary<string, object?>>(OldValuesJson)
     ?? new Dictionary<string, object?>();

    public void SetOldValues(Dictionary<string, object?> values) =>
        OldValuesJson = JsonSerializer.Serialize(values);
    public void SetOldValue(string key, object? value)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(OldValuesJson)
                    ?? new Dictionary<string, object?>();

        dict[key] = value;

        OldValuesJson = JsonSerializer.Serialize(dict);
    }

    // --- NewValues ---

    [NotMapped]
    public IReadOnlyDictionary<string, object?> NewValues =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(NewValuesJson)
        ?? new Dictionary<string, object?>();

    public void SetNewValues(Dictionary<string, object?> values) =>
        NewValuesJson = JsonSerializer.Serialize(values);

    public void SetNewValue(string key, object? value)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(NewValuesJson)
                    ?? new Dictionary<string, object?>();

        dict[key] = value;

        NewValuesJson = JsonSerializer.Serialize(dict);
    }

    // --- ModifiedProperties ---

    [NotMapped]
    public IReadOnlyCollection<string> ModifiedProperties =>
        JsonSerializer.Deserialize<Collection<string>>(ModifiedPropertiesJson)
        ?? new Collection<string>();

    public void SetModifiedProperties(Collection<string> values) =>
        ModifiedPropertiesJson = JsonSerializer.Serialize(values);

    public void AddModifiedProperty(string propertyName)
    {
        var list = JsonSerializer.Deserialize<Collection<string>>(ModifiedPropertiesJson)
                   ?? new Collection<string>();

        if (!list.Contains(propertyName))
            list.Add(propertyName);

        ModifiedPropertiesJson = JsonSerializer.Serialize(list);
    }

}
