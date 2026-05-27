using FluentValidation;
using FSH.Modules.Files.Contracts.v1.Commands;

namespace FSH.Modules.Files.Features.v1.RequestUploadUrl;

public sealed class RequestUploadUrlCommandValidator : AbstractValidator<RequestUploadUrlCommand>
{
    public RequestUploadUrlCommandValidator()
    {
        RuleFor(x => x.OwnerType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.Visibility).InclusiveBetween(0, 1);
    }
}
