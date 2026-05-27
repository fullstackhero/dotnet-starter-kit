using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.MarkChannelRead;

public static class MarkChannelReadEndpoint
{
    internal static RouteHandlerBuilder MapMarkChannelReadEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/channels/{id:guid}/read",
                async (Guid id, [FromBody] MarkChannelReadBody body, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new MarkChannelReadCommand(id, body.MessageId), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("MarkChannelRead")
            .WithSummary("Advance the caller's last-read marker in a channel")
            .RequirePermission(ChatPermissions.Channels.View);

    public sealed record MarkChannelReadBody(Guid MessageId);
}
