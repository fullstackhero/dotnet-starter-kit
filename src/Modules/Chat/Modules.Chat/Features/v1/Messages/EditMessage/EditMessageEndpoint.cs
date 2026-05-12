using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Messages.EditMessage;

public static class EditMessageEndpoint
{
    internal static RouteHandlerBuilder MapEditMessageEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/messages/{id:guid}",
                async (Guid id, [FromBody] EditMessageBody body, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new EditMessageCommand(id, body.Body), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdateMessage")
            .WithSummary("Edit a message — author only")
            .RequirePermission(ChatPermissions.Messages.EditOwn);

    public sealed record EditMessageBody(string Body);
}
