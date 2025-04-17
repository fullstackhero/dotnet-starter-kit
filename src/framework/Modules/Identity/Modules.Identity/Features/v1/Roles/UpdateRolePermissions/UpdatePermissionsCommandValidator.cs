using FluentValidation;
using FSH.Framework.Modules.Identity.Contracts.v1.Roles.UpdatePermissions;

namespace FSH.Framework.Identity.Endpoints.v1.Roles.UpdatePermissions;
public class UpdatePermissionsCommandValidator : AbstractValidator<UpdatePermissionsCommand>
{
    public UpdatePermissionsCommandValidator()
    {
        RuleFor(r => r.RoleId)
            .NotEmpty();
        RuleFor(r => r.Permissions)
            .NotNull();
    }
}