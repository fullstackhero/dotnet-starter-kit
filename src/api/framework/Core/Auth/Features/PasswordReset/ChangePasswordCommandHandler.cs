using MediatR;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, string>
{
    private readonly IUserRepository _userRepository;

    public ChangePasswordCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<string> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Domain validation
        if (!request.IsValid())
        {
            throw new ValidationException("Geçersiz TC Kimlik No veya şifre formatı");
        }

        var tcKimlik = request.GetTcKimlik();
        var currentPassword = request.GetCurrentPassword();
        var newPassword = request.GetPassword();

        // Validate current password first
        var (isValidCredentials, user) = await _userRepository.ValidatePasswordAndGetByTcknAsync(tcKimlik.Value, currentPassword.Value)
            .ConfigureAwait(false);

        if (!isValidCredentials || user == null)
        {
            throw new FshException("Mevcut şifreniz yanlış. Lütfen kontrol edip tekrar deneyiniz.");
        }

        // Check if new password was recently used
        var isPasswordRecentlyUsed = await _userRepository.IsPasswordRecentlyUsedAsync(tcKimlik.Value, newPassword.Value)
            .ConfigureAwait(false);

        if (isPasswordRecentlyUsed)
        {
            throw new FshException("Yeni şifreniz son üç (3) şifrenizden farklı olmalıdır!");
        }

        // Update password with history tracking
        await _userRepository.UpdatePasswordWithHistoryAsync(tcKimlik.Value, newPassword.Value)
            .ConfigureAwait(false);

        return "Şifreniz başarılı bir şekilde değiştirilmiştir.";
    }
} 