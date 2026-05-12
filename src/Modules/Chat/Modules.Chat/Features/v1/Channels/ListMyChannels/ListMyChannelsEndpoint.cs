using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.ListMyChannels;

public static class ListMyChannelsEndpoint
{
    internal static RouteHandlerBuilder MapListMyChannelsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/channels",
                async (int? page, int? pageSize, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new ListMyChannelsQuery(page ?? 1, pageSize ?? 50), cancellationToken)))
            .WithName("ListMyChannels")
            .WithSummary("List channels the current user is a member of, newest activity first")
            .RequirePermission(ChatPermissions.Channels.View);
}
