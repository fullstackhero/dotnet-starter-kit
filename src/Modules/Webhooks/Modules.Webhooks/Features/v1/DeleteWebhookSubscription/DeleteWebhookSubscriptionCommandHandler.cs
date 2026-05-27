using FSH.Framework.Core.Exceptions;
using FSH.Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;
using FSH.Modules.Webhooks.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Webhooks.Features.v1.DeleteWebhookSubscription;

public sealed class DeleteWebhookSubscriptionCommandHandler(
    WebhookDbContext dbContext) : ICommandHandler<DeleteWebhookSubscriptionCommand>
{
    public async ValueTask<Unit> Handle(DeleteWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Webhook subscription {command.Id} not found.");

        dbContext.Subscriptions.Remove(subscription);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
