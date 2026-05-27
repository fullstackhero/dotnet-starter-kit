using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.UpdateChannel;

public static class UpdateChannelEndpoint
{
    internal static RouteHandlerBuilder MapUpdateChannelEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/channels/{id:guid}",
                async (Guid id, [FromBody] UpdateChannelBody body, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new UpdateChannelCommand(id, body.Name, body.Description, body.IsPrivate), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdateChannel")
            .WithSummary("Rename / re-describe / re-privacy a named channel (channel admin only)")
            .RequirePermission(ChatPermissions.Channels.Create);

    public sealed record UpdateChannelBody(string Name, string? Description, bool IsPrivate);
}
