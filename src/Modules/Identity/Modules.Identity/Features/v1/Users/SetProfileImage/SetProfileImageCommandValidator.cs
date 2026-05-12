using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Users.SetProfileImage;

namespace FSH.Modules.Identity.Features.v1.Users.SetProfileImage;

public sealed class SetProfileImageCommandValidator : AbstractValidator<SetProfileImageCommand>
{
    public SetProfileImageCommandValidator()
    {
        // Empty/null is allowed (clears the image). When set, must look like a URL or relative path.
        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
    }
}
