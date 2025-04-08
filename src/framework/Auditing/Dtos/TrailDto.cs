using System.Collections.ObjectModel;
using FSH.Framework.Auditing.Enums;

namespace FSH.Framework.Auditing.Dtos;
public class TrailDto
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
}
