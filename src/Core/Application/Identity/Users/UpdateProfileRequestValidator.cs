using DN.WebApi.Application.Common.FileStorage;
using DN.WebApi.Application.Common.Validation;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Users;

public class UpdateProfileRequestValidator : CustomValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(p => p.FirstName).MaximumLength(75).NotEmpty();
        RuleFor(p => p.LastName).MaximumLength(75).NotEmpty();
        RuleFor(p => p.Email).NotEmpty();
        RuleFor(p => p.Image).SetNonNullableValidator(new FileUploadRequestValidator());
    }
}