namespace FSH.Framework.Core.Audit;
public class AuditTrail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Operation { get; set; }
    public string? Entity { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string? PreviousValues { get; set; }
    public string? NewValues { get; set; }
    public string? ModifiedProperties { get; set; }
    public string? PrimaryKey { get; set; }
}
