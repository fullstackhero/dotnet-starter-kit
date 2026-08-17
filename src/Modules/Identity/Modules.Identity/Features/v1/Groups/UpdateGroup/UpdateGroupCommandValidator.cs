using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Groups.UpdateGroup;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Groups.UpdateGroup;

public sealed class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(_ => localizer["Validation.GroupIdRequired"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.GroupNameRequired"])
            .MaximumLength(256).WithMessage(_ => localizer["Validation.GroupNameMaxLength"]);

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage(_ => localizer["Validation.DescriptionMaxLength"]);
    }
}