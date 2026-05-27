using FluentValidation;
using FSH.Modules.Files.Contracts.v1.Commands;

namespace FSH.Modules.Files.Features.v1.RestoreFile;

public sealed class RestoreFileCommandValidator : AbstractValidator<RestoreFileCommand>
{
    public RestoreFileCommandValidator()
    {
        RuleFor(x => x.FileAssetId).NotEmpty();
    }
}
