using System;
using FluentValidation;
using FSH.Framework.Core.Auth.Features.Identity;

namespace FSH.Framework.Core.Auth.Features.Identity;

public class TestMernisRequestValidator : AbstractValidator<TestMernisRequest>
{
    public TestMernisRequestValidator()
    {
        RuleFor(x => x.Tckn)
            .NotEmpty().WithMessage("TC Kimlik No is required")
            .Length(11).WithMessage("TC Kimlik No must be 11 digits")
            .Matches("^[0-9]*$").WithMessage("TC Kimlik No must contain only digits");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters")
            .Matches("^[a-zA-ZğüşıöçĞÜŞİÖÇ\\s]*$").WithMessage("First name can only contain letters and spaces");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters")
            .Matches("^[a-zA-ZğüşıöçĞÜŞİÖÇ\\s]*$").WithMessage("Last name can only contain letters and spaces");

        RuleFor(x => x.BirthYear)
            .NotEmpty().WithMessage("Birth year is required")
            .InclusiveBetween(1900, DateTime.Now.Year).WithMessage("Birth year must be between 1900 and current year");
    }
}