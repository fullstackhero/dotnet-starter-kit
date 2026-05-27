using FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Domain;
using Mediator;

namespace FSH.Modules.Webhooks.Features.v1.CreateWebhookSubscription;

public sealed class CreateWebhookSubscriptionCommandHandler(
    WebhookDbContext dbContext) : ICommandHandler<CreateWebhookSubscriptionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = WebhookSubscription.Create(command.Url, command.Events, command.Secret);
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return subscription.Id;
    }
}
