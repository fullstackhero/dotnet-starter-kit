using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Groups.CreateGroup;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Groups.CreateGroup;

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.GroupNameRequired"])
            .MaximumLength(256).WithMessage(_ => localizer["Validation.GroupNameMaxLength"]);

        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage(_ => localizer["Validation.DescriptionMaxLength"]);
    }
}