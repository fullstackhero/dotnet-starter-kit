using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAuditSummary;
using FSH.Modules.Auditing.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Auditing.Features.v1.GetAuditSummary;

public sealed class GetAuditSummaryQueryHandler : IQueryHandler<GetAuditSummaryQuery, AuditSummaryAggregateDto>
{
    private readonly AuditDbContext _dbContext;

    public GetAuditSummaryQueryHandler(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<AuditSummaryAggregateDto> Handle(GetAuditSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var audits = ApplyFilters(_dbContext.AuditRecords.AsNoTracking(), query);
        var list = await audits.ToListAsync(cancellationToken).ConfigureAwait(false);

        return AggregateRecords(list);
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

    private static AuditSummaryAggregateDto AggregateRecords(List<AuditRecord> records)
    {
        var aggregate = new AuditSummaryAggregateDto();

        foreach (var record in records)
        {
            AggregateByType(aggregate, record);
            AggregrateBySeverity(aggregate, record);
            AggregateBySource(aggregate, record);
            AggregateByTenant(aggregate, record);
        }

        return aggregate;
    }

    private static void AggregateByType(AuditSummaryAggregateDto aggregate, AuditRecord record)
    {
        var type = (AuditEventType)record.EventType;
        aggregate.EventsByType[type] = aggregate.EventsByType.TryGetValue(type, out var c) ? c + 1 : 1;
    }

    private static void AggregrateBySeverity(AuditSummaryAggregateDto aggregate, AuditRecord record)
    {
        var severity = (AuditSeverity)record.Severity;
        aggregate.EventsBySeverity[severity] = aggregate.EventsBySeverity.TryGetValue(severity, out var s) ? s + 1 : 1;
    }

    private static void AggregateBySource(AuditSummaryAggregateDto aggregate, AuditRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Source))
        {
            aggregate.EventsBySource[record.Source] = aggregate.EventsBySource.TryGetValue(record.Source, out var cs) ? cs + 1 : 1;
        }
    }

    private static void AggregateByTenant(AuditSummaryAggregateDto aggregate, AuditRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.TenantId))
        {
            aggregate.EventsByTenant[record.TenantId] = aggregate.EventsByTenant.TryGetValue(record.TenantId, out var ct) ? ct + 1 : 1;
        }
    }
}

