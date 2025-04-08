using FluentValidation;

namespace FSH.Framework.Core.Storage;

public class FileUploadRequestValidator : AbstractValidator<FileUploadRequest>
{
    public FileUploadRequestValidator(FileType fileType)
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
