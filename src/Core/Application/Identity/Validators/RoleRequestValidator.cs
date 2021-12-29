using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Shared.DTOs.Identity;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Validators;

public class RoleRequestValidator : CustomValidator<RoleRequest>
{
    public RoleRequestValidator()
    {
        RuleFor(p => p.Name).NotEmpty();
    }
}