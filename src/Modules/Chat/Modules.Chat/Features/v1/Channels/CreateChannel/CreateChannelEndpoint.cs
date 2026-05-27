using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.CreateChannel;

public static class CreateChannelEndpoint
{
    internal static RouteHandlerBuilder MapCreateChannelEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/channels",
                async (CreateChannelCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(command, cancellationToken)))
            .WithName("CreateChannel")
            .WithSummary("Create a new named chat channel")
            .RequirePermission(ChatPermissions.Channels.Create);
}
