using FluentValidation;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Domain;

namespace FSH.Modules.Files.Features.v1.ChangeVisibility;

public sealed class ChangeFileVisibilityCommandValidator : AbstractValidator<ChangeFileVisibilityCommand>
{
    public ChangeFileVisibilityCommandValidator()
    {
        RuleFor(x => x.FileAssetId).NotEmpty();

        RuleFor(x => x.Visibility)
            .Must(v => v is (int)Visibility.Public or (int)Visibility.Private)
            .WithMessage("Visibility must be Public (0) or Private (1).");
    }
}
