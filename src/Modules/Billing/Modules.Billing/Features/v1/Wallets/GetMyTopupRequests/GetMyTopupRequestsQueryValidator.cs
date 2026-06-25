using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Wallets;

namespace FSH.Modules.Billing.Features.v1.Wallets.GetMyTopupRequests;

public sealed class GetMyTopupRequestsQueryValidator : AbstractValidator<GetMyTopupRequestsQuery>
{
    public GetMyTopupRequestsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
