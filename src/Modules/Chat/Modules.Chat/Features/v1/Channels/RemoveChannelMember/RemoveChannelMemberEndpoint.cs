using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.RemoveChannelMember;

public static class RemoveChannelMemberEndpoint
{
    internal static RouteHandlerBuilder MapRemoveChannelMemberEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/channels/{id:guid}/members/{userId}",
                async (Guid id, string userId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new RemoveChannelMemberCommand(id, userId), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("RemoveChannelMember")
            .WithSummary("Remove a member (admin) or leave the channel (self)")
            .RequirePermission(ChatPermissions.Channels.View);
}
