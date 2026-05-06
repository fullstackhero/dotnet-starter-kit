using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.GetTenantSessions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Sessions.GetTenantSessions;

public sealed class GetTenantSessionsQueryHandler(ISessionService sessionService)
    : IQueryHandler<GetTenantSessionsQuery, PagedResponse<UserSessionDto>>
{
    public async ValueTask<PagedResponse<UserSessionDto>> Handle(
        GetTenantSessionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var (items, total) = await sessionService.GetTenantSessionsAsync(
            includeInactive: query.IncludeInactive,
            search: query.Search,
            skip: (page - 1) * size,
            take: size,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<UserSessionDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }
}
