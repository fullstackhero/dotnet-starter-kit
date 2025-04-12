using FSH.Framework.Auditing.Contracts.Enums;
using System.Collections.ObjectModel;

namespace FSH.Framework.Auditing.Contracts.Dtos;
public record TrailDto(
    Guid Id,
    DateTimeOffset DateTime,
    Guid UserId,
    AuditOperation Operation,
    string Description,
    string EntityName,
    Dictionary<string, object?> KeyValues,
    Dictionary<string, object?> OldValues,
    Dictionary<string, object?> NewValues,
    Collection<string> ModifiedProperties
);