using FSH.Framework.Core.Exceptions;
using FSH.Modules.Webhooks.Contracts.v1.TestWebhookSubscription;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FSH.Modules.Webhooks.Features.v1.TestWebhookSubscription;

public sealed class TestWebhookSubscriptionCommandHandler(
    WebhookDbContext dbContext,
    IWebhookDeliveryService deliveryService) : ICommandHandler<TestWebhookSubscriptionCommand, bool>
{
    public async ValueTask<bool> Handle(TestWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Webhook subscription {command.Id} not found.");

        var testPayload = JsonSerializer.Serialize(new
        {
            eventType = "webhook.test",
            timestamp = TimeProvider.System.GetUtcNow().UtcDateTime,
            message = "This is a test webhook delivery."
        });

        await deliveryService.DeliverAsync(
            subscription.Id,
            subscription.Url,
            subscription.SecretHash,
            "webhook.test",
            testPayload,
            cancellationToken).ConfigureAwait(false);

        return true;
    }
}
