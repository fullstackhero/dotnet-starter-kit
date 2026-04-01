using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Groups.UpdateGroup;

namespace FSH.Modules.Identity.Features.v1.Groups.UpdateGroup;

public sealed class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Group ID"));

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Group name"))
            .MaximumLength(256).WithMessage(IdentityValidationMessages.MaxLength("Group name", 256));

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage(IdentityValidationMessages.MaxLength("Description", 1024));
    }
}