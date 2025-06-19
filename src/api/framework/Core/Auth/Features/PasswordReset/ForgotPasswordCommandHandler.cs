using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Common.Exceptions;
using FSH.Framework.Core.Common.Options;
using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
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

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing forgot password request for: {TcknOrMemberNumber}", request.TcknOrMemberNumber);

        // Domain validation
        if (!request.IsValid())
        {
            throw new ValidationException("Geçersiz TC Kimlik No/Üye No veya Doğum Tarihi");
        }

        try
        {
            (bool isValid, AppUser? user) validationResult;
            (string? email, string? phone) contactInfo;

            // Determine if input is TCKN (11 digits) or Member Number
            if (request.TcknOrMemberNumber.Length == 11 && request.TcknOrMemberNumber.All(char.IsDigit))
            {
                // Validate with TCKN
                validationResult = await _userRepository.ValidateTcKimlikAndBirthDateAsync(request.TcknOrMemberNumber, request.BirthDate);
                contactInfo = await _userRepository.GetUserContactInfoAsync(request.TcknOrMemberNumber);
            }
            else
            {
                // Validate with Member Number
                validationResult = await _userRepository.ValidateMemberNumberAndBirthDateAsync(request.TcknOrMemberNumber, request.BirthDate);
                contactInfo = await _userRepository.GetUserContactInfoByMemberNumberAsync(request.TcknOrMemberNumber);
            }

            if (!validationResult.isValid || validationResult.user == null)
            {
                // Return specific error messages
                bool userExists;
                
                if (request.TcknOrMemberNumber.Length == 11 && request.TcknOrMemberNumber.All(char.IsDigit))
                {
                    userExists = await _userRepository.TcKimlikExistsAsync(request.TcknOrMemberNumber);
                }
                else
                {
                    var memberUser = await _userRepository.GetByMemberNumberAsync(request.TcknOrMemberNumber);
                    userExists = memberUser != null;
                }

                if (!userExists)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Böyle bir kullanıcı bulunmamaktadır. Lütfen kontrol edip tekrar deneyiniz."
                    };
                }
                else
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Kimlik bilgileriniz sistemdeki bilgilerle eşleşmiyor. Lütfen kontrol edip tekrar deneyiniz."
                    };
                }
            }

            _logger.LogInformation("TC Kimlik/Member Number and birth date validation successful for: {TcknOrMemberNumber}", request.TcknOrMemberNumber);

            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "Bilgileriniz doğrulandı. Lütfen şifre sıfırlama yöntemini seçiniz.",
                MaskedEmail = !string.IsNullOrEmpty(contactInfo.email) ? MaskEmail(contactInfo.email) : null,
                MaskedPhone = !string.IsNullOrEmpty(contactInfo.phone) ? MaskPhone(contactInfo.phone) : null,
                HasEmail = !string.IsNullOrEmpty(contactInfo.email),
                HasPhone = !string.IsNullOrEmpty(contactInfo.phone)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request for: {TcknOrMemberNumber}", request.TcknOrMemberNumber);
            throw new FshException("Şifre sıfırlama talebiniz işlenirken bir hata oluştu. Lütfen tekrar deneyiniz.");
        }
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        
        var username = parts[0];
        var domain = parts[1];
        
        if (username.Length <= 2) return email;
        
        var maskedUsername = username[0] + new string('*', username.Length - 2) + username[^1];
        return $"{maskedUsername}@{domain}";
    }

    private static string MaskPhone(string phone)
    {
        // Remove any non-digit characters
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        if (digits.Length < 10) return phone;
        
        // Format as masked phone: 0(5**) *** ** **
        return $"0({digits[1]}{new string('*', 2)}) {new string('*', 3)} {new string('*', 2)} {digits[^2..]}";
    }
}

public sealed class SelectResetMethodCommandHandler : IRequestHandler<SelectResetMethodCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SelectResetMethodCommandHandler> _logger;
    private readonly FrontendOptions _frontendOptions;

    public SelectResetMethodCommandHandler(
        IUserRepository userRepository,
        IPasswordResetService passwordResetService,
        ISmsService smsService,
        IEmailService emailService,
        ILogger<SelectResetMethodCommandHandler> logger,
        IOptions<FrontendOptions> frontendOptions)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordResetService = passwordResetService ?? throw new ArgumentNullException(nameof(passwordResetService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _frontendOptions = frontendOptions?.Value ?? throw new ArgumentNullException(nameof(frontendOptions));
    }

    public async Task<string> Handle(SelectResetMethodCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ValidationException("Geçersiz istek parametreleri");
        }

        // Re-validate identity and birth date
        (bool isValid, AppUser? user) validationResult;
        (string? email, string? phone) contactInfo;

        // Determine if input is TCKN (11 digits) or Member Number
        if (request.TcknOrMemberNumber.Length == 11 && request.TcknOrMemberNumber.All(char.IsDigit))
        {
            validationResult = await _userRepository.ValidateTcKimlikAndBirthDateAsync(request.TcknOrMemberNumber, request.BirthDate);
            contactInfo = await _userRepository.GetUserContactInfoAsync(request.TcknOrMemberNumber);
        }
        else
        {
            validationResult = await _userRepository.ValidateMemberNumberAndBirthDateAsync(request.TcknOrMemberNumber, request.BirthDate);
            contactInfo = await _userRepository.GetUserContactInfoByMemberNumberAsync(request.TcknOrMemberNumber);
        }
        
        if (!validationResult.isValid || validationResult.user == null)
        {
            throw new FshException("Kimlik bilgileri doğrulanamadı");
        }

        string target;
        string maskedTarget;
        
        if (request.Method == ResetMethod.Email)
        {
            if (string.IsNullOrEmpty(contactInfo.email))
            {
                throw new FshException("Bu kullanıcı için email adresi kayıtlı değil");
            }
            target = contactInfo.email;
            maskedTarget = MaskEmail(contactInfo.email);
        }
        else // SMS
        {
            if (string.IsNullOrEmpty(contactInfo.phone))
            {
                throw new FshException("Bu kullanıcı için telefon numarası kayıtlı değil");
            }
            target = contactInfo.phone;
            maskedTarget = MaskPhone(contactInfo.phone);
        }

        // Generate reset token using the identifier (TC or Member Number)
        var resetToken = await _passwordResetService.GenerateResetTokenAsync(request.TcknOrMemberNumber);

        try
        {
            if (request.Method == ResetMethod.Email)
            {
                var resetLink = $"{_frontendOptions.BaseUrl}/auth/reset-password?token={resetToken}";
                await _emailService.SendPasswordResetEmailAsync(target, resetLink);
                _logger.LogInformation("Password reset email sent to masked address: {MaskedEmail}", maskedTarget);
                return $"Şifre sıfırlama bağlantısı {maskedTarget} adresine gönderildi.";
            }
            else
            {
                var resetLink = $"{_frontendOptions.BaseUrl}/auth/reset-password?token={resetToken}";
                var smsMessage = $"Şifre sıfırlama bağlantınız: {resetLink}";
                await _smsService.SendSmsAsync(target, smsMessage);
                _logger.LogInformation("Password reset SMS sent to masked phone: {MaskedPhone}", maskedTarget);
                return $"Şifre sıfırlama bağlantısı {maskedTarget} numarasına gönderildi.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset via {Method} to {Target}", request.Method, maskedTarget);
            throw new FshException($"Şifre sıfırlama {(request.Method == ResetMethod.Email ? "e-postası" : "SMS'i")} gönderilemedi. Lütfen tekrar deneyiniz.");
        }
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        
        var username = parts[0];
        var domain = parts[1];
        
        if (username.Length <= 2) return email;
        
        var maskedUsername = username[0] + new string('*', username.Length - 2) + username[^1];
        return $"{maskedUsername}@{domain}";
    }

    private static string MaskPhone(string phone)
    {
        // Remove any non-digit characters
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        if (digits.Length < 10) return phone;
        
        // Format as masked phone: 0(5**) *** ** **
        return $"0({digits[1]}{new string('*', 2)}) {new string('*', 3)} {new string('*', 2)} {digits[^2..]}";
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
        var (isValid, user) = await _userRepository.ValidateTcKimlikAndPhoneAsync(tcKimlik.Value, phoneNumber.Value);
        
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
        await _smsService.GenerateAndStoreSmsCodeAsync(phoneNumber.Value);
        
        _logger.LogInformation("SMS code sent for password reset to: {Phone}", phoneNumber.Value);
        
        return "SMS kodu telefon numaranıza gönderildi. Kodu kullanarak yeni şifrenizi belirleyebilirsiniz.";
    }
}