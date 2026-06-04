using FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Domain;
using FSH.Modules.Webhooks.Services;
using Mediator;

namespace FSH.Modules.Webhooks.Features.v1.CreateWebhookSubscription;

public sealed class CreateWebhookSubscriptionCommandHandler(
    WebhookDbContext dbContext,
    IWebhookSecretProtector secretProtector) : ICommandHandler<CreateWebhookSubscriptionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Encrypt the signing secret at rest — it is the HMAC key, so it must be recoverable
        // (not hashed). Decrypted only at dispatch time to sign the outbound payload.
        var protectedSecret = secretProtector.Protect(command.Secret);
        var subscription = WebhookSubscription.Create(command.Url, command.Events, protectedSecret);
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return subscription.Id;
    }
}
