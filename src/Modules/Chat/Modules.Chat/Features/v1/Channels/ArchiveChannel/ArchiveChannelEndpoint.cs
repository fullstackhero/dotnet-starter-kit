using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.ArchiveChannel;

public static class ArchiveChannelEndpoint
{
    internal static RouteHandlerBuilder MapArchiveChannelEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/channels/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new ArchiveChannelCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("ArchiveChannel")
            .WithSummary("Soft-archive a channel (channel admin only)")
            .RequirePermission(ChatPermissions.Channels.Create);
}
