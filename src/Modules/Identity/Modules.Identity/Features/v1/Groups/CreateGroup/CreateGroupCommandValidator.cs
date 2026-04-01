using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Groups.CreateGroup;

namespace FSH.Modules.Identity.Features.v1.Groups.CreateGroup;

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(IdentityValidationMessages.GroupNameRequired)
            .MaximumLength(256).WithMessage(IdentityValidationMessages.GroupNameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage(IdentityValidationMessages.DescriptionMaxLength);
    }
}