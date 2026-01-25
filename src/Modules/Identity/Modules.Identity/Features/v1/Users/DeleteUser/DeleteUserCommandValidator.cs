using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Users.DeleteUser;

namespace FSH.Modules.Identity.Features.v1.Users.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
