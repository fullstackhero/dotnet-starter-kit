using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Messages.DeleteMessage;

public static class DeleteMessageEndpoint
{
    internal static RouteHandlerBuilder MapDeleteMessageEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/messages/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteMessageCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteMessage")
            .WithSummary("Delete a message — author can delete own; moderators can delete any")
            .RequirePermission(ChatPermissions.Messages.DeleteOwn);
}
