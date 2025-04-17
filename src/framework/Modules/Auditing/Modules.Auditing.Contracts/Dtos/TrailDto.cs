using FSH.Framework.Auditing.Contracts.Enums;
using System.Collections.ObjectModel;

namespace FSH.Framework.Auditing.Contracts.Dtos;
public class TrailDto
{
    public Guid Id { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public Guid UserId { get; set; }
    public AuditOperation Operation { get; set; }
    public string Description { get; set; } = default!;
    public string EntityName { get; set; } = default!;

    // Uncomment if needed later
    public Dictionary<string, object?> KeyValues { get; set; } = new();
    public Dictionary<string, object?> OldValues { get; set; } = new();
    public Dictionary<string, object?> NewValues { get; set; } = new();
    public Collection<string> ModifiedProperties { get; set; } = new();
}