using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Users.ResendConfirmationEmail;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Users.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailCommandValidator : AbstractValidator<ResendConfirmationEmailCommand>
{
    public ResendConfirmationEmailCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(_ => localizer["Validation.UserIdRequired"]);
    }
}
