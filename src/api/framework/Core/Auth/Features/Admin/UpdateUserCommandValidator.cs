using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.Admin;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .Must(email => Email.Create(email.Value).IsSuccess)
            .WithMessage("Invalid email format");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Must(username => Username.Create(username.Value).IsSuccess)
            .WithMessage("Invalid username format");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Must(phone => PhoneNumber.Create(phone.Value).IsSuccess)
            .WithMessage("Invalid phone number format");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Must(name => Name.Create(name.Value).IsSuccess)
            .WithMessage("Invalid first name format");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Must(name => Name.Create(name.Value).IsSuccess)
            .WithMessage("Invalid last name format");

        RuleFor(x => x.Profession)
            .Must(profession => profession == null || Profession.Create(profession.Value).IsSuccess)
            .WithMessage("Invalid profession format");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => x == "ACTIVE" || x == "INACTIVE" || x == "SUSPENDED")
            .WithMessage("Status must be one of: ACTIVE, INACTIVE, SUSPENDED");
    }
} 