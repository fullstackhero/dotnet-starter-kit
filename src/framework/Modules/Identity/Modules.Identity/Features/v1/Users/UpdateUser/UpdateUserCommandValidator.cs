using FluentValidation;
using FSH.Framework.Core.Storage;
using FSH.Framework.Identity.Contracts.v1.Users.UpdateUser;

namespace FSH.Framework.Identity.v1.Users.UpdateUser;
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.FirstName)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(15)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        When(x => x.Image is not null, () =>
        {
            RuleFor(x => x.Image!)
                .SetValidator(new UserImageValidator(FileType.Image));
        });

        // Prevent deleting and uploading image at the same time
        RuleFor(x => x)
            .Must(x => !(x.DeleteCurrentImage && x.Image is not null))
            .WithMessage("You cannot upload a new image and delete the current one simultaneously.");
    }
}