using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Groups.AddUsersToGroup;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Groups.AddUsersToGroup;

public sealed class AddUsersToGroupCommandValidator : AbstractValidator<AddUsersToGroupCommand>
{
    public AddUsersToGroupCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage(_ => localizer["Validation.GroupIdRequired"]);

        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage(_ => localizer["Validation.AtLeastOneUserIdRequired"])
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage(_ => localizer["Validation.UserIdsNotEmptyOrWhitespace"]);
    }
}