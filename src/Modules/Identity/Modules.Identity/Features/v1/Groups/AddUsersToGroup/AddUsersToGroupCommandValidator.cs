using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Groups.AddUsersToGroup;

namespace FSH.Modules.Identity.Features.v1.Groups.AddUsersToGroup;

public sealed class AddUsersToGroupCommandValidator : AbstractValidator<AddUsersToGroupCommand>
{
    public AddUsersToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required.");

        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user ID is required.")
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("User IDs cannot be empty or whitespace.");
    }
}
