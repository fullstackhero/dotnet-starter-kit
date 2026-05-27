using FluentValidation;
using FSH.Modules.Webhooks.Contracts.v1.TestWebhookSubscription;

namespace FSH.Modules.Webhooks.Features.v1.TestWebhookSubscription;

public sealed class TestWebhookSubscriptionCommandValidator : AbstractValidator<TestWebhookSubscriptionCommand>
{
    public TestWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
