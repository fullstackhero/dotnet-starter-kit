using FSH.Modules.Webhooks.Contracts.v1.GetWebhookSubscriptions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Webhooks.Features.v1.GetWebhookSubscriptions;

public static class GetWebhookSubscriptionsEndpoint
{
    internal static RouteHandlerBuilder MapGetWebhookSubscriptionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/subscriptions", async (
            int pageNumber,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWebhookSubscriptionsQuery(pageNumber, pageSize), ct);
            return TypedResults.Ok(result);
        })
        .WithName("GetWebhookSubscriptions")
        .WithSummary("List webhook subscriptions");
    }
}
