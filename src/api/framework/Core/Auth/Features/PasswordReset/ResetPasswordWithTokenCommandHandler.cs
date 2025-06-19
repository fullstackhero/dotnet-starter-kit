using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ResetPasswordWithTokenCommandHandler : IRequestHandler<ResetPasswordWithTokenCommand, string>
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ResetPasswordWithTokenCommandHandler> _logger;

    public ResetPasswordWithTokenCommandHandler(
        IPasswordResetService passwordResetService,
        IUserRepository userRepository,
        ILogger<ResetPasswordWithTokenCommandHandler> logger)
    {
        _passwordResetService = passwordResetService ?? throw new ArgumentNullException(nameof(passwordResetService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(ResetPasswordWithTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ValidationException("Geçersiz token veya şifre formatı");
        }

        _logger.LogInformation("Processing password reset with token: {Token}", request.Token[..8] + "...");

        try
        {
            // Validate token
            var isTokenValid = await _passwordResetService.ValidateResetTokenAsync(request.Token);
            
            if (!isTokenValid)
            {
                _logger.LogWarning("Invalid or expired token used for password reset: {Token}", request.Token[..8] + "...");
                throw new FshException("Token geçersiz veya süresi dolmuş. Lütfen yeni bir şifre sıfırlama talebinde bulunun.");
            }

            // Get identifier (TCKN or Member Number) from token
            var identifier = await _passwordResetService.GetIdentifierFromTokenAsync(request.Token);
            
            if (string.IsNullOrEmpty(identifier))
            {
                _logger.LogError("Could not find identifier for token: {Token}", request.Token[..8] + "...");
                throw new FshException("Token ile ilişkili kullanıcı bulunamadı.");
            }

            // Get password domain object
            var password = request.GetPassword();

            // Reset password using the service (automatically detects TCKN vs Member Number)
            await _passwordResetService.ResetUserPasswordByIdentifierAsync(identifier, password.Value);

            // Invalidate the token after successful password reset
            await _passwordResetService.InvalidateResetTokenAsync(identifier);

            _logger.LogInformation("Password successfully reset for identifier: {Identifier}", identifier);

            return "Şifreniz başarıyla güncellendi. Artık yeni şifrenizle giriş yapabilirsiniz.";
        }
        catch (FshException)
        {
            throw; // Re-throw business exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token: {Token}", request.Token[..8] + "...");
            throw new FshException("Şifre sıfırlanırken bir hata oluştu. Lütfen tekrar deneyiniz.");
        }
    }


} 