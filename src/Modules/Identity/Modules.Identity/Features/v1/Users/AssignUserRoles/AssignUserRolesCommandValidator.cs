using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Users.AssignUserRoles;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Users.AssignUserRoles;

public sealed class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(_ => localizer["Validation.UserIdRequired"]);

        RuleFor(x => x.UserRoles)
            .NotNull().WithMessage(_ => localizer["Validation.UserRolesRequired"]);
    }
}