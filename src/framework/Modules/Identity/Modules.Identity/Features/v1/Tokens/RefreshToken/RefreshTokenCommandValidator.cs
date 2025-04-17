using FluentValidation;
using FSH.Framework.Identity.Core.Tokens;

namespace FSH.Framework.Identity.v1.Tokens.RefreshToken;
internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(p => p.Token).Cascade(CascadeMode.Stop).NotEmpty();
        RuleFor(p => p.RefreshToken).Cascade(CascadeMode.Stop).NotEmpty();
    }
}