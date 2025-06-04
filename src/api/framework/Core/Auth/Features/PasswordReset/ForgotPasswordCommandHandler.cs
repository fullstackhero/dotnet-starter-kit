using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing forgot password request for TC Kimlik: {TcKimlik}", request.TcKimlikNo);

        // Domain validation
        var tcKimlik = request.GetTcKimlik();
        
        // Check if TC Kimlik exists in system (but don't reveal if it exists or not for security)
        await _userRepository.TcKimlikExistsAsync(tcKimlik.Value).ConfigureAwait(false);
        
        _logger.LogInformation("TC Kimlik existence check completed for: {TcKimlik}", request.TcKimlikNo);
        
        // Always return success message (security: don't reveal if TC exists)
        return "Eğer bu TC kimlik numarası sistemde kayıtlı ise, telefon numarası girmeniz için sonraki adıma geçebilirsiniz.";
    }
}

public sealed class ValidateTcPhoneCommandHandler : IRequestHandler<ValidateTcPhoneCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly ISmsService _smsService;
    private readonly ILogger<ValidateTcPhoneCommandHandler> _logger;

    public ValidateTcPhoneCommandHandler(
        IUserRepository userRepository,
        ISmsService smsService,
        ILogger<ValidateTcPhoneCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(ValidateTcPhoneCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing TC Kimlik + Phone validation for: {TcKimlik}", request.TcKimlikNo);

        // Domain validation
        var tcKimlik = request.GetTcKimlik();
        var phoneNumber = request.GetPhoneNumber();
        
        // Check if TC Kimlik + Phone match
        var (isValid, user) = await _userRepository.ValidateTcKimlikAndPhoneAsync(tcKimlik.Value, phoneNumber.Value)
            .ConfigureAwait(false);
        
        if (!isValid || user == null)
        {
            _logger.LogWarning(
                "TC Kimlik and Phone validation failed for: {TcKimlik}, {PhoneNumber}",
                request.TcKimlikNo,
                request.PhoneNumber);

            // Security: Don't reveal specific error
            throw new FshException("TC Kimlik ve telefon numarası eşleşmiyor.");
        }

        // Generate and send SMS code
        await _smsService.GenerateAndStoreSmsCodeAsync(phoneNumber.Value)
            .ConfigureAwait(false);
        
        _logger.LogInformation("SMS code sent for password reset to: {Phone}", phoneNumber.Value);
        
        return "SMS kodu telefon numaranıza gönderildi. Kodu kullanarak yeni şifrenizi belirleyebilirsiniz.";
    }
} 