using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;

namespace FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;

public sealed class GenerateTokenCommandValidator : AbstractValidator<GenerateTokenCommand>
{
    public GenerateTokenCommandValidator()
    {
        RuleFor(p => p.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        RuleFor(p => p.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}