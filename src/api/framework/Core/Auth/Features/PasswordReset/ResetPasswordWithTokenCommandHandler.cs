using MediatR;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ResetPasswordWithTokenCommandHandler : IRequestHandler<ResetPasswordWithTokenCommand, string>
{
    private readonly IPasswordResetService _passwordResetService;

    public ResetPasswordWithTokenCommandHandler(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService ?? throw new ArgumentNullException(nameof(passwordResetService));
    }

    public async Task<string> Handle(ResetPasswordWithTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ValidationException("Geçersiz token veya şifre formatı");
        }

        try
        {
            // Validate token
            var isTokenValid = await _passwordResetService.ValidateResetTokenAsync(request.Token);
            
            if (!isTokenValid)
            {
                throw new FshException("Token geçersiz veya süresi dolmuş. Lütfen yeni bir şifre sıfırlama talebinde bulunun.");
            }

            // Get identifier (TCKN or Member Number) from token
            var identifier = await _passwordResetService.GetIdentifierFromTokenAsync(request.Token);
            
            if (string.IsNullOrEmpty(identifier))
            {
                throw new FshException("Token ile ilişkili kullanıcı bulunamadı.");
            }

            // Get password domain object
            var password = request.GetPassword();

            // Reset password using the service (automatically detects TCKN vs Member Number)
            await _passwordResetService.ResetUserPasswordByIdentifierAsync(identifier, password.Value);

            // Invalidate the token after successful password reset
            await _passwordResetService.InvalidateResetTokenAsync(identifier);

            return "Şifreniz başarıyla güncellendi. Artık yeni şifrenizle giriş yapabilirsiniz.";
        }
        catch (FshException)
        {
            throw; // Re-throw business exceptions
        }
        catch (Exception)
        {
            throw new FshException("Şifre sıfırlanırken bir hata oluştu. Lütfen tekrar deneyiniz.");
        }
    }
}