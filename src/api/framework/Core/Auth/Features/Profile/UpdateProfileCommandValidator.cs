using FluentValidation;

namespace FSH.Framework.Core.Auth.Features.Profile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x).Must(cmd => !string.IsNullOrEmpty(cmd.Username) || !string.IsNullOrEmpty(cmd.Profession))
            .WithMessage("At least one field must be provided");
    }
} 