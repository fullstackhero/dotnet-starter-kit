using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Groups.AddUsersToGroup;

namespace FSH.Modules.Identity.Features.v1.Groups.AddUsersToGroup;

public sealed class AddUsersToGroupCommandValidator : AbstractValidator<AddUsersToGroupCommand>
{
    public AddUsersToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithMessage(IdentityValidationMessages.GroupIdRequired);

        RuleFor(x => x.UserIds)
            .NotEmpty()
            .WithMessage(IdentityValidationMessages.UserIdsRequired)
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage(IdentityValidationMessages.UserIdsInvalid);
    }
}
