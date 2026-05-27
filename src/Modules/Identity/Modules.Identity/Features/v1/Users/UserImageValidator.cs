using FluentValidation;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;

namespace FSH.Modules.Identity.Features.v1.Users;

public sealed class UserImageValidator : AbstractValidator<FileUploadRequest>
{
    public UserImageValidator() : this(FileType.Image) { }
    public UserImageValidator(FileType fileType)
    {
        var rules = FileTypeMetadata.GetRules(fileType);

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(file => rules.AllowedExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"Only these extensions are allowed: {string.Join(", ", rules.AllowedExtensions)}");

        RuleFor(x => x.Data)
            .NotEmpty()
            .Must(data => data.Count <= rules.MaxSizeInMB * 1024 * 1024)
            .WithMessage($"File must be <= {rules.MaxSizeInMB} MB.");
    }
}