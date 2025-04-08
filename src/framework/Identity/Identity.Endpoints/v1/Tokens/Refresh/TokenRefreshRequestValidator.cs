using FluentValidation;
using FSH.Framework.Identity.Core.Tokens;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Refresh;
public class TokenRefreshRequestValidator : AbstractValidator<TokenRefreshRequest>
{
    public TokenRefreshRequestValidator()
    {
        RuleFor(p => p.Token).Cascade(CascadeMode.Stop).NotEmpty();

        RuleFor(p => p.RefreshToken).Cascade(CascadeMode.Stop).NotEmpty();
    }
}
