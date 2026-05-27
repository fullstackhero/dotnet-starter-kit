using FluentValidation;
using FSH.Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;

namespace FSH.Modules.Webhooks.Features.v1.DeleteWebhookSubscription;

public sealed class DeleteWebhookSubscriptionCommandValidator : AbstractValidator<DeleteWebhookSubscriptionCommand>
{
    public DeleteWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
