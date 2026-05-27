using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.RestoreChannel;

public static class RestoreChannelEndpoint
{
    internal static RouteHandlerBuilder MapRestoreChannelEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/channels/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new RestoreChannelCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("RestoreChannel")
            .WithSummary("Restore an archived channel (admin moderation)")
            .RequirePermission(ChatPermissions.Channels.ManageAll);
}
