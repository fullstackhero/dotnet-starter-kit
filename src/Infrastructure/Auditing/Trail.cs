using FL_CRMS_ERP_WEBAPI.Domain.Common.Contracts;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Auditing;

public class Trail : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Type { get; set; }
    public string? TableName { get; set; }
    public DateTime DateTime { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? AffectedColumns { get; set; }
    public string? PrimaryKey { get; set; }
}