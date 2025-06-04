using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, string>
{
    private readonly ISmsService _smsService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        ISmsService smsService,
        IUserRepository userRepository,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing password reset for TC Kimlik: {TcKimlik}", request.TcKimlikNo);

        // Domain validation
        var tcKimlik = request.GetTcKimlik();
        var phoneNumber = request.GetPhoneNumber();
        var newPassword = request.GetPassword();

        // Validate SMS code
        var isValidSmsCode = await _smsService.ValidateSmsCodeAsync(phoneNumber.Value, request.SmsCode)
            .ConfigureAwait(false);

        if (!isValidSmsCode)
        {
            _logger.LogWarning(
                "Invalid SMS code used for password reset: {TcKimlik}, {PhoneNumber}",
                request.TcKimlikNo,
                request.PhoneNumber);

            throw new FshException("SMS kodu geçersiz veya süresi dolmuş.");
        }

        // Validate TC Kimlik + Phone match again for security
        var (isValid, user) = await _userRepository.ValidateTcKimlikAndPhoneAsync(tcKimlik.Value, phoneNumber.Value)
            .ConfigureAwait(false);

        if (!isValid || user == null)
        {
            _logger.LogWarning(
                "TC Kimlik and Phone validation failed during password reset: {TcKimlik}, {PhoneNumber}",
                request.TcKimlikNo,
                request.PhoneNumber);

            throw new FshException("TC Kimlik ve telefon numarası eşleşmiyor.");
        }

        // Update password using BCrypt
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword.Value);
        await _userRepository.UpdatePasswordAsync(user.Id, hashedPassword)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Password successfully reset for user: {UserId}, {TcKimlik}, {PhoneNumber}",
            user.Id,
            request.TcKimlikNo,
            request.PhoneNumber);

        return "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz.";
    }
} 