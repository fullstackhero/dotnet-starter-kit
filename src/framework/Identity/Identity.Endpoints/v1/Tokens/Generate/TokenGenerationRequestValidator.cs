using FluentValidation;
using FSH.Framework.Identity.Core.Dtos;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Generate;
public class TokenGenerationRequestValidator : AbstractValidator<TokenGenerationRequest>
{
    public TokenGenerationRequestValidator()
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
