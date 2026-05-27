using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Reactions.AddReaction;

public static class AddReactionEndpoint
{
    internal static RouteHandlerBuilder MapAddReactionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/messages/{id:guid}/reactions",
                async (Guid id, [FromBody] AddReactionBody body, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new AddReactionCommand(id, body.Emoji), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("AddReaction")
            .WithSummary("Toggle on a reaction emoji on a message")
            .RequirePermission(ChatPermissions.Messages.Send);

    public sealed record AddReactionBody(string Emoji);
}
