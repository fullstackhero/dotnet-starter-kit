namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Generate;
using FluentValidation;

public sealed class TokenGenerationValidator : AbstractValidator<TokenGenerationCommand>
{
    public TokenGenerationValidator()
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
