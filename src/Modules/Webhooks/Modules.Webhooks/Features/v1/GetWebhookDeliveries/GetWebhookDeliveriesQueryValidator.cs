using FluentValidation;
using FSH.Modules.Webhooks.Contracts.v1.GetWebhookDeliveries;

namespace FSH.Modules.Webhooks.Features.v1.GetWebhookDeliveries;

public sealed class GetWebhookDeliveriesQueryValidator : AbstractValidator<GetWebhookDeliveriesQuery>
{
    public GetWebhookDeliveriesQueryValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
