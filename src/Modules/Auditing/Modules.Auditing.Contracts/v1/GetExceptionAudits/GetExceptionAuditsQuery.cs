using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetExceptionAudits;

public sealed class GetExceptionAuditsQuery : IQuery<IReadOnlyList<AuditSummaryDto>>
{
    public ExceptionArea? Area { get; init; }

    public AuditSeverity? Severity { get; init; }

    public string? ExceptionType { get; init; }

    public string? RouteOrLocation { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}