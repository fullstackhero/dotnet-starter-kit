using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Groups.CreateGroup;

namespace FSH.Modules.Identity.Features.v1.Groups.CreateGroup;

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(256).WithMessage("Group name must not exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage("Description must not exceed 1024 characters.");
    }
}