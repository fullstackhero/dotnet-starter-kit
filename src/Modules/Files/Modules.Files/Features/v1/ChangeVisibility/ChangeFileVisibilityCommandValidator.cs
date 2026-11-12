using FluentValidation;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Files.Features.v1.ChangeVisibility;

public sealed class ChangeFileVisibilityCommandValidator : AbstractValidator<ChangeFileVisibilityCommand>
{
    public ChangeFileVisibilityCommandValidator(IStringLocalizer<FilesResources> localizer)
    {
        RuleFor(x => x.FileAssetId).NotEmpty();

        RuleFor(x => x.Visibility)
            .IsInEnum()
            .WithMessage(_ => localizer["Files.VisibilityInvalid"]);
    }
}
