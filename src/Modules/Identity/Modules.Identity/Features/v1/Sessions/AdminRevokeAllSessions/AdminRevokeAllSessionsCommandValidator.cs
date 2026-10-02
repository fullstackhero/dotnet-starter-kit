using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeAllSessions;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;

public sealed class AdminRevokeAllSessionsCommandValidator : AbstractValidator<AdminRevokeAllSessionsCommand>
{
    public AdminRevokeAllSessionsCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(_ => localizer["Validation.UserIdRequired"]);

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage(_ => localizer["Validation.ReasonMaxLength"])
            .When(x => x.Reason is not null);
    }
}