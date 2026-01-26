using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetExceptionAudits;
using FSH.Modules.Auditing.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Auditing.Features.v1.GetExceptionAudits;

public sealed class GetExceptionAuditsQueryHandler : IQueryHandler<GetExceptionAuditsQuery, IReadOnlyList<AuditSummaryDto>>
{
    private readonly AuditDbContext _dbContext;

    public GetExceptionAuditsQueryHandler(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyList<AuditSummaryDto>> Handle(GetExceptionAuditsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var audits = GetBaseQuery();
        audits = ApplyDateFilters(audits, query);
        audits = ApplySeverityFilter(audits, query);
        audits = ApplyPayloadFilters(audits, query);

        return await ProjectToDto(audits, cancellationToken);
    }

    private IQueryable<AuditRecord> GetBaseQuery()
    {
        return _dbContext.AuditRecords
            .AsNoTracking()
            .Where(a => a.EventType == (int)AuditEventType.Exception);
    }

    private static IQueryable<AuditRecord> ApplyDateFilters(IQueryable<AuditRecord> audits, GetExceptionAuditsQuery query)
    {
        if (query.FromUtc.HasValue)
        {
            audits = audits.Where(a => a.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            audits = audits.Where(a => a.OccurredAtUtc <= query.ToUtc.Value);
        }

        return audits;
    }

    private static IQueryable<AuditRecord> ApplySeverityFilter(IQueryable<AuditRecord> audits, GetExceptionAuditsQuery query)
    {
        if (query.Severity.HasValue)
        {
            audits = audits.Where(a => a.Severity == (byte)query.Severity.Value);
        }

        return audits;
    }

    private static IQueryable<AuditRecord> ApplyPayloadFilters(IQueryable<AuditRecord> audits, GetExceptionAuditsQuery query)
    {
        if (query.Area.HasValue && query.Area.Value != ExceptionArea.None)
        {
            string areaValue = query.Area.Value.ToString();
            audits = audits.Where(a => a.PayloadJson != null &&
                EF.Functions.ILike(a.PayloadJson, $"%\"area\":\"{areaValue}\"%"));
        }

        if (!string.IsNullOrWhiteSpace(query.ExceptionType))
        {
            audits = audits.Where(a => a.PayloadJson != null &&
                EF.Functions.ILike(a.PayloadJson, $"%\"exceptionType\":\"{query.ExceptionType}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.RouteOrLocation))
        {
            audits = audits.Where(a => a.PayloadJson != null &&
                EF.Functions.ILike(a.PayloadJson, $"%\"routeOrLocation\":\"{query.RouteOrLocation}%"));
        }

        return audits;
    }

    private static async Task<IReadOnlyList<AuditSummaryDto>> ProjectToDto(IQueryable<AuditRecord> audits, CancellationToken cancellationToken)
    {
        return await audits
            .OrderByDescending(a => a.OccurredAtUtc)
            .Select(a => new AuditSummaryDto
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
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

