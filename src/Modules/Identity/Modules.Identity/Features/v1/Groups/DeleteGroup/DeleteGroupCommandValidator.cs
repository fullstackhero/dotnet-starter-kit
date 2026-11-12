using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Groups.DeleteGroup;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Groups.DeleteGroup;

public sealed class DeleteGroupCommandValidator : AbstractValidator<DeleteGroupCommand>
{
    public DeleteGroupCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(_ => localizer["Validation.GroupIdRequired"]);
    }
}