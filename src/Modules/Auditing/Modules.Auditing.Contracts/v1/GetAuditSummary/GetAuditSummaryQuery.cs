using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAuditSummary;

public sealed class GetAuditSummaryQuery : IQuery<AuditSummaryAggregateDto>
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? TenantId { get; init; }
}