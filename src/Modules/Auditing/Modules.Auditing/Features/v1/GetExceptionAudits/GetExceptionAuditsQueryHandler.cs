using FSH.Framework.Persistence.Providers;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetExceptionAudits;
using FSH.Modules.Auditing.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using static FSH.Modules.Auditing.Persistence.AuditJsonbFunctions;

namespace FSH.Modules.Auditing.Features.v1.GetExceptionAudits;

public sealed class GetExceptionAuditsQueryHandler : IQueryHandler<GetExceptionAuditsQuery, IReadOnlyList<AuditSummaryDto>>
{
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 50;

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
        audits = ApplyPayloadFilters(audits, query, _dbContext.Database);

        // Cap server-side so an unpaged call can't materialize a tenant's whole exception history.
        var take = query.Take is >= 1 and <= MaxPageSize ? query.Take.Value : DefaultPageSize;
        var skip = query.Skip is > 0 ? query.Skip.Value : 0;

        return await ProjectToDto(audits, skip, take, cancellationToken);
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

    private static IQueryable<AuditRecord> ApplyPayloadFilters(
        IQueryable<AuditRecord> audits,
        GetExceptionAuditsQuery query,
        DatabaseFacade database)
    {
        if (query.Area.HasValue && query.Area.Value != ExceptionArea.None)
        {
            string areaValue = query.Area.Value.ToString();
            audits = audits.WhereLike(
                database,
                ProviderQueryExtensions.JsonTextPropertyPattern(database, "area", areaValue),
                a => AsText(a.PayloadJson));
        }

        if (!string.IsNullOrWhiteSpace(query.ExceptionType))
        {
            audits = audits.WhereLike(
                database,
                ProviderQueryExtensions.JsonTextPropertyPattern(database, "exceptionType", query.ExceptionType, exact: false),
                a => AsText(a.PayloadJson));
        }

        if (!string.IsNullOrWhiteSpace(query.RouteOrLocation))
        {
            audits = audits.WhereLike(
                database,
                ProviderQueryExtensions.JsonTextPropertyPattern(database, "routeOrLocation", query.RouteOrLocation, exact: false),
                a => AsText(a.PayloadJson));
        }

        return audits;
    }

    private static async Task<IReadOnlyList<AuditSummaryDto>> ProjectToDto(IQueryable<AuditRecord> audits, int skip, int take, CancellationToken cancellationToken)
    {
        return await audits
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip(skip)
            .Take(take)
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