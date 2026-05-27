using FSH.Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Webhooks.Features.v1.DeleteWebhookSubscription;

public static class DeleteWebhookSubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapDeleteWebhookSubscriptionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/subscriptions/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DeleteWebhookSubscriptionCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName("DeleteWebhookSubscription")
        .WithSummary("Delete a webhook subscription")
        .Produces(StatusCodes.Status204NoContent);
    }
}
