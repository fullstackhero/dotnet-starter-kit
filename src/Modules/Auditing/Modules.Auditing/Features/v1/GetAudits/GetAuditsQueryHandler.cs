using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Authorization;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAudits;
using FSH.Modules.Auditing.Persistence;
using FSH.Modules.Identity.Contracts.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using static FSH.Modules.Auditing.Persistence.AuditJsonbFunctions;

namespace FSH.Modules.Auditing.Features.v1.GetAudits;

public sealed class GetAuditsQueryHandler : IQueryHandler<GetAuditsQuery, PagedResponse<AuditSummaryDto>>
{
    /// <summary>
    /// Maximum window allowed when the caller supplies a from/to. We refuse
    /// to scan the entire table — without this guard, an unconstrained query
    /// degenerates into a full sequential scan as the audit volume grows.
    /// </summary>
    public static readonly TimeSpan MaxWindow = TimeSpan.FromDays(90);

    /// <summary>
    /// Default lookback when the caller does not supply a from/to. Keeps the
    /// happy-path query bounded for the dashboard.
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromDays(7);

    private readonly AuditDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUserPermissionService _permissions;
    private readonly TimeProvider _timeProvider;

    public GetAuditsQueryHandler(
        AuditDbContext dbContext,
        ICurrentUser currentUser,
        IUserPermissionService permissions,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _permissions = permissions;
        _timeProvider = timeProvider;
    }

    public async ValueTask<PagedResponse<AuditSummaryDto>> Handle(GetAuditsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (fromUtc, toUtc) = ResolveWindow(query.FromUtc, query.ToUtc);

        var audits = await BuildBaseQueryAsync(query, cancellationToken).ConfigureAwait(false);

        audits = audits.Where(a => a.OccurredAtUtc >= fromUtc && a.OccurredAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            audits = audits.Where(a => a.UserId == query.UserId);
        }

        if (query.EventType.HasValue)
        {
            audits = audits.Where(a => a.EventType == (int)query.EventType.Value);
        }

        if (query.ExcludeEventType.HasValue)
        {
            audits = audits.Where(a => a.EventType != (int)query.ExcludeEventType.Value);
        }

        if (query.Severity.HasValue)
        {
            audits = audits.Where(a => a.Severity == (byte)query.Severity.Value);
        }

        if (query.Tags.HasValue && query.Tags.Value != AuditTag.None)
        {
            long tagMask = (long)query.Tags.Value;
            audits = audits.Where(a => (a.Tags & tagMask) != 0);
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

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search;
            // ILIKE on PayloadJson is sequential without a GIN/trigram index.
            // The composite (TenantId, OccurredAtUtc) index keeps the planner
            // honest by scoping the scan; pair this with a GIN index on
            // PayloadJson in production for sub-second search.
            audits = audits.Where(a =>
                (a.PayloadJson != null && EF.Functions.ILike(AsText(a.PayloadJson), $"%{term}%")) ||
                (a.Source != null && EF.Functions.ILike(a.Source, $"%{term}%")) ||
                (a.UserName != null && EF.Functions.ILike(a.UserName, $"%{term}%")));
        }

        audits = audits.OrderByDescending(a => a.OccurredAtUtc);

        IQueryable<AuditSummaryDto> projected = audits.Select(a => new AuditSummaryDto
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
        });

        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a queryable already scoped to the right tenant. If the caller
    /// supplied a TenantId equal to their own, that's a no-op. Cross-tenant
    /// access requires the explicit ViewCrossTenant permission and bypasses
    /// Finbuckle's anonymous tenant filter, then re-applies an explicit
    /// TenantId predicate so we never accidentally return rows for *all*
    /// tenants.
    /// </summary>
    private async Task<IQueryable<AuditRecord>> BuildBaseQueryAsync(GetAuditsQuery query, CancellationToken ct)
    {
        var currentTenant = _currentUser.GetTenant();
        var requested = string.IsNullOrWhiteSpace(query.TenantId) ? null : query.TenantId;

        bool wantsCrossTenant =
            requested is not null
            && !string.Equals(requested, currentTenant, StringComparison.OrdinalIgnoreCase);

        if (!wantsCrossTenant)
        {
            return _dbContext.AuditRecords.AsNoTracking();
        }

        var userId = _currentUser.GetUserId().ToString();
        var allowed = await _permissions
            .HasPermissionAsync(userId, AuditingPermissions.AuditTrails.ViewCrossTenant, ct)
            .ConfigureAwait(false);
        if (!allowed)
        {
            throw new ForbiddenException("Cross-tenant audit access requires Permissions.AuditTrails.ViewCrossTenant.");
        }

        return _dbContext.AuditRecords
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == requested);
    }

    /// <summary>
    /// Clamps the supplied window to <see cref="MaxWindow"/> and supplies a
    /// <see cref="DefaultWindow"/> when both endpoints are missing. The
    /// validator catches obvious misuse (from &gt; to); this method handles
    /// the open-ended "no range" case so the SQL is always bounded.
    /// </summary>
    private (DateTime FromUtc, DateTime ToUtc) ResolveWindow(DateTime? from, DateTime? to)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resolvedTo = to ?? now;
        var resolvedFrom = from ?? resolvedTo - DefaultWindow;

        if (resolvedTo - resolvedFrom > MaxWindow)
        {
            resolvedFrom = resolvedTo - MaxWindow;
        }

        return (resolvedFrom, resolvedTo);
    }
}
