using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Reactions.RemoveReaction;

public static class RemoveReactionEndpoint
{
    internal static RouteHandlerBuilder MapRemoveReactionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/messages/{id:guid}/reactions/{emoji}",
                async (Guid id, string emoji, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new RemoveReactionCommand(id, Uri.UnescapeDataString(emoji)), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("RemoveReaction")
            .WithSummary("Toggle off a reaction emoji on a message")
            .RequirePermission(ChatPermissions.Messages.Send);
}
