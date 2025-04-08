using FluentValidation;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Refresh;
public record RefreshTokenCommand(string Token, string RefreshToken);

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(p => p.Token).Cascade(CascadeMode.Stop).NotEmpty();

        RuleFor(p => p.RefreshToken).Cascade(CascadeMode.Stop).NotEmpty();
    }
}
