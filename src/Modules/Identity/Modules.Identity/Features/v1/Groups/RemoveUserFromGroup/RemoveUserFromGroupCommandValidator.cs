using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public sealed class RemoveUserFromGroupCommandValidator : AbstractValidator<RemoveUserFromGroupCommand>
{
    public RemoveUserFromGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage(IdentityValidationMessages.GroupIdRequired);

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.UserIdRequired);
    }
}