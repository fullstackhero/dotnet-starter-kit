using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Groups.UpdateGroup;

namespace FSH.Modules.Identity.Features.v1.Groups.UpdateGroup;

public sealed class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(256).WithMessage("Group name must not exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage("Description must not exceed 1024 characters.");
    }
}