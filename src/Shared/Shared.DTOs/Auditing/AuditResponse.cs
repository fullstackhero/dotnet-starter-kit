using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Auditing;

[DataContract]
public class AuditResponse
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public Guid UserId { get; set; }

    [DataMember(Order = 3)]
    public string? Type { get; set; }

    [DataMember(Order = 4)]
    public string? TableName { get; set; }

    [DataMember(Order = 5)]
    public DateTime DateTime { get; set; }

    [DataMember(Order = 6)]
    public string? OldValues { get; set; }

    [DataMember(Order = 7)]
    public string? NewValues { get; set; }

    [DataMember(Order = 8)]
    public string? AffectedColumns { get; set; }

    [DataMember(Order = 9)]
    public string? PrimaryKey { get; set; }
}