using FluentValidation;
using FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Localization;
using FSH.Modules.Webhooks.Services;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Webhooks.Features.v1.CreateWebhookSubscription;

public sealed class CreateWebhookSubscriptionCommandValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionCommandValidator(IStringLocalizer<WebhooksResources> localizer)
    {
        RuleFor(x => x.Url).NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage(_ => localizer["Validation.WebhookUrlInvalid"])
            .Must(url => !Uri.TryCreate(url, UriKind.Absolute, out var uri) || !WebhookUrlGuard.IsBlockedHost(uri.Host))
            .WithMessage(_ => localizer["Validation.WebhookUrlBlockedTarget"]);
        RuleFor(x => x.Events).NotEmpty().WithMessage(_ => localizer["Validation.WebhookEventsRequired"]);
    }
}
