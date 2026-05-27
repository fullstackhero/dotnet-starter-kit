using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.DiscoverChannels;

public static class DiscoverChannelsEndpoint
{
    internal static RouteHandlerBuilder MapDiscoverChannelsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/channels/discover",
                async (string? search, int? page, int? pageSize, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new DiscoverChannelsQuery(search, page ?? 1, pageSize ?? 50), cancellationToken)))
            .WithName("DiscoverChannels")
            .WithSummary("List public channels the current user is NOT yet in")
            .RequirePermission(ChatPermissions.Channels.View);
}
