using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Users.AssignUserRoles;

namespace FSH.Modules.Identity.Features.v1.Users.AssignUserRoles;

public sealed class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.UserIdRequired);

        RuleFor(x => x.UserRoles)
            .NotNull().WithMessage(IdentityValidationMessages.UserRolesRequired);
    }
}