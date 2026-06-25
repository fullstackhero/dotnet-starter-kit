using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Wallets;

namespace FSH.Modules.Billing.Features.v1.Wallets.ApproveTopupRequest;

public sealed class ApproveTopupRequestCommandValidator : AbstractValidator<ApproveTopupRequestCommand>
{
    public ApproveTopupRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
