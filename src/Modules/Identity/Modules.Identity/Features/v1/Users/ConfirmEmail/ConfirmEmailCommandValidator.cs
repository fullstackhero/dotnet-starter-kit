using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(_ => localizer["Validation.UserIdRequired"]);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(_ => localizer["Validation.ConfirmationCodeRequired"]);

        RuleFor(x => x.Tenant)
            .NotEmpty().WithMessage(_ => localizer["Validation.TenantRequired"]);
    }
}