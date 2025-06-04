using FluentValidation;

namespace FSH.Framework.Core.Auth.Features.Admin;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => role == "admin" || role == "customer_admin" || role == "customer_support" || role == "base_user")
            .WithMessage("Invalid role. Must be one of: admin, customer_admin, customer_support, base_user");
    }
} 