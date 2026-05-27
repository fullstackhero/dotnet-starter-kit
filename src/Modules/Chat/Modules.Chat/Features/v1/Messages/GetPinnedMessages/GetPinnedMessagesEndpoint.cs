using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Messages.GetPinnedMessages;

public static class GetPinnedMessagesEndpoint
{
    internal static RouteHandlerBuilder MapGetPinnedMessagesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/channels/{id:guid}/pinned",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetPinnedMessagesQuery(id), cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("ListPinnedMessages")
            .WithSummary("Pinned messages in a channel — most recently pinned first")
            .RequirePermission(ChatPermissions.Channels.View);
}
