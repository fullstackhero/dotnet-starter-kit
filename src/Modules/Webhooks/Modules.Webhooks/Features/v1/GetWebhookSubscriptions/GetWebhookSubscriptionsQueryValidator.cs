using FluentValidation;
using FSH.Modules.Webhooks.Contracts.v1.GetWebhookSubscriptions;

namespace FSH.Modules.Webhooks.Features.v1.GetWebhookSubscriptions;

public sealed class GetWebhookSubscriptionsQueryValidator : AbstractValidator<GetWebhookSubscriptionsQuery>
{
    public GetWebhookSubscriptionsQueryValidator()
    {
        // PageSize must be >= 1 — a 0 reaches Math.Ceiling(total / (double)PageSize) and would
        // otherwise surface as a 500 instead of a clean 400.
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
