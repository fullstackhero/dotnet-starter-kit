using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Messages.ListMessageReplies;

public static class ListMessageRepliesEndpoint
{
    internal static RouteHandlerBuilder MapListMessageRepliesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/messages/{id:guid}/replies",
                async (Guid id, Guid? before, int? pageSize, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(
                        new ListMessageRepliesQuery(id, before, pageSize ?? 50),
                        cancellationToken)))
            .WithName("ListMessageReplies")
            .WithSummary("List replies to a thread parent message (newest first, cursor-paged)")
            .RequirePermission(ChatPermissions.Channels.View);
}
