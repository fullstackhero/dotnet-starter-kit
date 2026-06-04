using FSH.Framework.Shared.Persistence;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAudits;

public sealed class GetAuditsQuery : IPagedQuery, IQuery<PagedResponse<AuditSummaryDto>>
{
    public int? PageNumber { get; set; }

    public int? PageSize { get; set; }

    public string? Sort { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public AuditEventType? EventType { get; set; }

    /// <summary>Hide a single event type (e.g. <c>Activity</c> to drop system-level HTTP noise).
    /// Applied as a not-equals filter so paging + totals stay correct.</summary>
    public AuditEventType? ExcludeEventType { get; set; }

    public AuditSeverity? Severity { get; set; }

    public AuditTag? Tags { get; set; }

    public string? Source { get; set; }

    public string? CorrelationId { get; set; }

    public string? TraceId { get; set; }

    public string? Search { get; set; }
}