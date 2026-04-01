using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAudits;
using FSH.Modules.Auditing.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Auditing.Features.v1.GetAudits;

public sealed class GetAuditsQueryHandler(AuditDbContext dbContext) : IQueryHandler<GetAuditsQuery, PagedResponse<AuditSummaryDto>>
{
    public async ValueTask<PagedResponse<AuditSummaryDto>> Handle(GetAuditsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<AuditRecord> audits = dbContext.AuditRecords.AsNoTracking();

        // Apply tenant filter first (indexed)
        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            audits = audits.Where(a => a.TenantId == query.TenantId);
        }

        // Apply time range filters (indexed with composite index)
        if (query.FromUtc.HasValue)
        {
            audits = audits.Where(a => a.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            audits = audits.Where(a => a.OccurredAtUtc <= query.ToUtc.Value);
        }

        // Apply indexed filters
        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            audits = audits.Where(a => a.UserId == query.UserId);
        }

        if (query.EventType.HasValue)
        {
            audits = audits.Where(a => a.EventType == (int)query.EventType.Value);
        }

        if (query.Severity.HasValue)
        {
            audits = audits.Where(a => a.Severity == (byte)query.Severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            audits = audits.Where(a => a.Source == query.Source);
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            audits = audits.Where(a => a.CorrelationId == query.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(query.TraceId))
        {
            audits = audits.Where(a => a.TraceId == query.TraceId);
        }

        if (query.Tags.HasValue && query.Tags.Value != AuditTag.None)
        {
            long tagMask = (long)query.Tags.Value;
            audits = audits.Where(a => (a.Tags & tagMask) != 0);
        }

        // Apply search last (most expensive operation)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search;
            audits = audits.Where(a =>
                (a.PayloadJson != null && EF.Functions.ILike(a.PayloadJson, $"%{term}%")) ||
                (a.Source != null && EF.Functions.ILike(a.Source, $"%{term}%")) ||
                (a.UserName != null && EF.Functions.ILike(a.UserName, $"%{term}%")));
        }

        audits = audits.OrderByDescending(a => a.OccurredAtUtc);

        return await audits.ToPagedResponseAsync(
            a => new AuditSummaryDto
            {
                Id = a.Id,
                OccurredAtUtc = a.OccurredAtUtc,
                EventType = (AuditEventType)a.EventType,
                Severity = (AuditSeverity)a.Severity,
                TenantId = a.TenantId,
                UserId = a.UserId,
                UserName = a.UserName,
                TraceId = a.TraceId,
                CorrelationId = a.CorrelationId,
                RequestId = a.RequestId,
                Source = a.Source,
                Tags = (AuditTag)a.Tags
            },
            query,
            cancellationToken).ConfigureAwait(false);
    }
}