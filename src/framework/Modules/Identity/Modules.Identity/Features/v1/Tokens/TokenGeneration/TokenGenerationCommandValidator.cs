using FluentValidation;
using FSH.Framework.Identity.Contracts.v1.Tokens.TokenGeneration;

namespace FSH.Framework.Identity.v1.Tokens.TokenGeneration;
public class TokenGenerationCommandValidator : AbstractValidator<TokenGenerationCommand>
{
    public TokenGenerationCommandValidator()
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