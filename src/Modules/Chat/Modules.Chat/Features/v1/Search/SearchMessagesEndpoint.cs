using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Search;

public static class SearchMessagesEndpoint
{
    internal static RouteHandlerBuilder MapSearchMessagesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/search",
                async (string q, Guid? channelId, int? page, int? pageSize, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(
                        new SearchMessagesQuery(q ?? string.Empty, channelId, page ?? 1, pageSize ?? 50),
                        cancellationToken)))
            .WithName("SearchMessages")
            .WithSummary("Full-text message search scoped to channels the caller is a member of")
            .RequirePermission(ChatPermissions.Channels.View);
}
