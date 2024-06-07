using FluentValidation;

namespace FSH.Framework.Core.Identity.Roles.Features.DeleteRole;

public class DeleteRoleValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Role ID is required.");
    }
}
