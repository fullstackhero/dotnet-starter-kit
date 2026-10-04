using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Users;

public sealed class UserImageValidator : AbstractValidator<FileUploadRequest>
{
    public UserImageValidator(IStringLocalizer<SharedResources> localizer) : this(FileType.Image, localizer) { }
    public UserImageValidator(FileType fileType, IStringLocalizer<SharedResources> localizer)
    {
        var rules = FileTypeMetadata.GetRules(fileType);

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(file => rules.AllowedExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage(_ => localizer["Validation.AllowedExtensions", string.Join(", ", rules.AllowedExtensions)]);

        RuleFor(x => x.Data)
            .NotEmpty()
            .Must(data => data.Count <= rules.MaxSizeInMB * 1024 * 1024)
            .WithMessage(_ => localizer["Validation.MaxFileSize", rules.MaxSizeInMB]);
    }
}