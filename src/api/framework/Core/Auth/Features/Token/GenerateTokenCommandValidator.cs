using FluentValidation;

namespace FSH.Framework.Core.Auth.Features.Token.Generate;

public class GenerateTokenCommandValidator : AbstractValidator<GenerateTokenCommand>
{
    public GenerateTokenCommandValidator()
    {
        RuleFor(x => x.Tckn)
            .NotEmpty().WithMessage("TC Kimlik No is required")
            .Length(11).WithMessage("TC Kimlik No must be 11 digits")
            .Matches(@"^\d{11}$").WithMessage("TC Kimlik No must contain only digits");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
} 