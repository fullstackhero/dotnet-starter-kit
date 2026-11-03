using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Roles.UpsertRole;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Roles.UpsertRole;

public sealed class UpsertRoleCommandValidator : AbstractValidator<UpsertRoleCommand>
{
    public UpsertRoleCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(_ => localizer["Validation.RoleNameRequired"]);
    }
}