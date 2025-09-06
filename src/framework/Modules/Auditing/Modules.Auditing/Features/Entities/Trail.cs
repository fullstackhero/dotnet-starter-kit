using FSH.Framework.Auditing.Contracts.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FSH.Framework.Auditing.Core.Entities;

public class Trail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public AuditOperation Operation { get; set; }
    public string Description { get; set; } = default!;
    public string? EntityName { get; set; }

    // Backing fields for JSON storage (persisted)
    public string KeyValuesJson { get; private set; } = "{}";
    public string OldValuesJson { get; private set; } = "{}";
    public string NewValuesJson { get; private set; } = "{}";
    public string ModifiedPropertiesJson { get; private set; } = "[]";

    // Domain-facing properties (not mapped)
    [NotMapped]
    public IReadOnlyDictionary<string, object?> KeyValues =>
        DeserializeDict(KeyValuesJson);

    [NotMapped]
    public IReadOnlyDictionary<string, object?> OldValues =>
        DeserializeDict(OldValuesJson);

    [NotMapped]
    public IReadOnlyDictionary<string, object?> NewValues =>
        DeserializeDict(NewValuesJson);

    [NotMapped]
    public IReadOnlyCollection<string> ModifiedProperties =>
        JsonSerializer.Deserialize<List<string>>(ModifiedPropertiesJson)
        ?? [];

    // Setters for domain logic
    public void SetKeyValues(Dictionary<string, object?> values) =>
        KeyValuesJson = Serialize(values);

    public void SetOldValues(Dictionary<string, object?> values) =>
        OldValuesJson = Serialize(values);

    public void SetNewValues(Dictionary<string, object?> values) =>
        NewValuesJson = Serialize(values);

    public void SetModifiedProperties(IEnumerable<string> properties) =>
        ModifiedPropertiesJson = JsonSerializer.Serialize(properties.Distinct().ToList());

    public void AddModifiedProperty(string property)
    {
        var props = ModifiedProperties.ToList();
        if (!props.Contains(property))
            props.Add(property);
        ModifiedPropertiesJson = JsonSerializer.Serialize(props);
    }

    // Helpers
    private static Dictionary<string, object?> DeserializeDict(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
    }

    private static string Serialize(object obj) =>
        JsonSerializer.Serialize(obj);
}