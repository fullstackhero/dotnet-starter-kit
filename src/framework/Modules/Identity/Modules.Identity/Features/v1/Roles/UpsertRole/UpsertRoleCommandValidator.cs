using FluentValidation;

namespace FSH.Framework.Identity.Endpoints.v1.Roles.CreateOrUpdateRole;

public class UpsertRoleCommandValidator : AbstractValidator<UpsertRoleCommand>
{
    public UpsertRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Role name is required.");
    }
}