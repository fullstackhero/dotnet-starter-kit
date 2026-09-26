using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public sealed class RemoveUserFromGroupCommandValidator : AbstractValidator<RemoveUserFromGroupCommand>
{
    public RemoveUserFromGroupCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage(_ => localizer["Validation.GroupIdRequired"]);

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(_ => localizer["Validation.UserIdRequired"]);
    }
}