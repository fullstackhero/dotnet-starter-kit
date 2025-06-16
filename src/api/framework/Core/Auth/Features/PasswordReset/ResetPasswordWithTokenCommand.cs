using MediatR;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed record ResetPasswordWithTokenCommand : IRequest<string>
{
    public string Token { get; init; } = default!;
    public string NewPassword { get; init; } = default!;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Token) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               Token.Length >= 10 &&
               Password.IsValid(NewPassword);
    }

    public Password GetPassword()
    {
        if (!Password.IsValid(NewPassword))
            throw new ValidationException("Geçersiz şifre formatı");
        
        return Password.CreateUnsafe(NewPassword);
    }
} 