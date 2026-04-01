using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Users.AssignUserRoles;

namespace FSH.Modules.Identity.Features.v1.Users.AssignUserRoles;

public sealed class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("User ID"));

        RuleFor(x => x.UserRoles)
            .NotNull().WithMessage(IdentityValidationMessages.Required("User roles list"));
    }
}