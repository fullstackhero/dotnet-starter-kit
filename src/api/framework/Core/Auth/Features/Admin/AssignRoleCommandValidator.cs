using System;
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
            .Must(role => string.Equals(role, "admin", StringComparison.Ordinal) || string.Equals(role, "customer_admin", StringComparison.Ordinal) || string.Equals(role, "customer_support", StringComparison.Ordinal) || string.Equals(role, "base_user", StringComparison.Ordinal))
            .WithMessage("Invalid role. Must be one of: admin, customer_admin, customer_support, base_user");
    }
}