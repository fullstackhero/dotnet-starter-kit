using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAuditsByTrace;

public sealed class GetAuditsByTraceQuery : IQuery<IReadOnlyList<AuditSummaryDto>>
{
    public string TraceId { get; init; } = default!;

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}