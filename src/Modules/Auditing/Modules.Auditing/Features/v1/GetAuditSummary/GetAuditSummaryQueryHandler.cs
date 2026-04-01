using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAuditSummary;
using FSH.Modules.Auditing.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Auditing.Features.v1.GetAuditSummary;

public sealed class GetAuditSummaryQueryHandler(AuditDbContext dbContext) : IQueryHandler<GetAuditSummaryQuery, AuditSummaryAggregateDto>
{
    public async ValueTask<AuditSummaryAggregateDto> Handle(GetAuditSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var audits = ApplyFilters(dbContext.AuditRecords.AsNoTracking(), query);

        // Push all aggregation to the database via GROUP BY — avoids loading any rows into memory.
        // EF Core DbContext is not thread-safe, so queries are run sequentially.

        var byType = await audits
            .GroupBy(a => a.EventType)
            .Select(g => new { EventType = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(g => (AuditEventType)g.EventType, g => g.Count, cancellationToken)
            .ConfigureAwait(false);

        var bySeverity = await audits
            .GroupBy(a => a.Severity)
            .Select(g => new { Severity = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(g => (AuditSeverity)g.Severity, g => g.Count, cancellationToken)
            .ConfigureAwait(false);

        var bySource = await audits
            .Where(a => a.Source != null)
            .GroupBy(a => a.Source!)
            .Select(g => new { Source = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(g => g.Source, g => g.Count, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        var byTenant = await audits
            .Where(a => a.TenantId != null)
            .GroupBy(a => a.TenantId!)
            .Select(g => new { TenantId = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(g => g.TenantId, g => g.Count, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        return new AuditSummaryAggregateDto
        {
            EventsByType = byType,
            EventsBySeverity = bySeverity,
            EventsBySource = bySource,
            EventsByTenant = byTenant
        };
    }

    private static IQueryable<AuditRecord> ApplyFilters(IQueryable<AuditRecord> audits, GetAuditSummaryQuery query)
    {
        if (query.FromUtc.HasValue)
        {
            audits = audits.Where(a => a.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            audits = audits.Where(a => a.OccurredAtUtc <= query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            audits = audits.Where(a => a.TenantId == query.TenantId);
        }

        return audits;
    }
}
