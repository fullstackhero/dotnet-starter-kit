namespace FSH.WebApi.Application.Identity.Users.Password;

public class ResetPasswordRequest
{
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string Token { get; set; } = default!;
}

public class ResetPasswordRequestValidator : CustomValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator(IStringLocalizer<ResetPasswordRequestValidator> T)
    {
        RuleFor(p => p.Password)
            .NotEmpty().NotNull();

        RuleFor(p => p.Email)
            .NotEmpty().NotNull();

        RuleFor(p => p.Token)
            .NotEmpty().NotNull();
    }
}