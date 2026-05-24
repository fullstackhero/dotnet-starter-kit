using FluentValidation;
using FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription;

namespace FSH.Modules.Webhooks.Features.v1.CreateWebhookSubscription;

public sealed class CreateWebhookSubscriptionCommandValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Url).NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid absolute URL is required.");
        RuleFor(x => x.Events).NotEmpty().WithMessage("At least one event type is required.");
    }
}
