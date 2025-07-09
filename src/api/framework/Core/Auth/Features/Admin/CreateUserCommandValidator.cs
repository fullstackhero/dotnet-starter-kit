using System;
using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.Admin;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
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

        RuleFor(x => x.Tckn)
            .NotEmpty().WithMessage("TCKN is required")
            .Must(tckn => Tckn.Create(tckn.Value).IsSuccess)
            .WithMessage("Invalid TCKN format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Must(password => Password.Create(password.Value).IsSuccess)
            .WithMessage("Invalid password format");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Must(name => Name.Create(name.Value).IsSuccess)
            .WithMessage("Invalid first name format");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Must(name => Name.Create(name.Value).IsSuccess)
            .WithMessage("Invalid last name format");

        RuleFor(x => x.ProfessionId)
            .Must(professionId => !professionId.HasValue || professionId > 0)
            .WithMessage("Invalid profession id");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Birth date is required")
            .Must(date => BirthDate.Create(date.Value).IsSuccess)
            .WithMessage("Invalid birth date format");

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrEmpty(status) || string.Equals(status, "ACTIVE", StringComparison.Ordinal) || string.Equals(status, "INACTIVE", StringComparison.Ordinal) || string.Equals(status, "SUSPENDED", StringComparison.Ordinal))
            .WithMessage("Status must be one of: ACTIVE, INACTIVE, SUSPENDED");
    }
}