using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.GetChannelById;

public static class GetChannelByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetChannelByIdEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/channels/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new GetChannelByIdQuery(id), cancellationToken)))
            .WithName("GetChannelById")
            .WithSummary("Get a single channel with members and unread count")
            .RequirePermission(ChatPermissions.Channels.View);
}
