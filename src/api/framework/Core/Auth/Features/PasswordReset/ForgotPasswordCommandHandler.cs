using MediatR;
using Microsoft.Extensions.Options;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Common.Exceptions;
using FSH.Framework.Core.Common.Options;
using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUserRepository _userRepository;

    public ForgotPasswordCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

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
    private readonly FrontendOptions _frontendOptions;

    public SelectResetMethodCommandHandler(
        IUserRepository userRepository,
        IPasswordResetService passwordResetService,
        ISmsService smsService,
        IEmailService emailService,
        IOptions<FrontendOptions> frontendOptions)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordResetService = passwordResetService ?? throw new ArgumentNullException(nameof(passwordResetService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
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

        try
        {
        // Generate reset token
            string token;
            if (request.Method == ResetMethod.Email)
            {
                token = await _passwordResetService.GenerateResetTokenAsync(target);
            }
            else
            {
                // For SMS, we might need a different approach or use the same token system
                token = await _passwordResetService.GenerateResetTokenAsync(target);
            }

            if (request.Method == ResetMethod.Email)
            {
                // Send reset email
                var resetLink = $"{_frontendOptions.BaseUrl}/auth/reset-password?token={token}";
                await _emailService.SendPasswordResetEmailAsync(target, resetLink);
                
                return $"Şifre sıfırlama bağlantısı {maskedTarget} adresine gönderildi.";
            }
            else
            {
                // Send SMS with reset link
                var resetLink = $"{_frontendOptions.BaseUrl}/auth/reset-password?token={token}";
                await _smsService.SendSmsAsync(target, $"Şifre sıfırlama bağlantınız: {resetLink}");
                
                return $"Şifre sıfırlama bağlantısı {maskedTarget} numarasına SMS ile gönderildi.";
            }
        }
        catch (Exception ex)
        {
            throw new FshException($"Şifre sıfırlama bağlantısı gönderilirken bir hata oluştu. Lütfen tekrar deneyiniz.");
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

    public ValidateTcPhoneCommandHandler(
        IUserRepository userRepository,
        ISmsService smsService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
    }

    public async Task<string> Handle(ValidateTcPhoneCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tcKimlik = Tckn.CreateUnsafe(request.TcKimlikNo);
        var phoneNumber = PhoneNumber.CreateUnsafe(request.PhoneNumber);
        
        // Validate TC Kimlik and Phone Number combination
        var (isValid, user) = await _userRepository.ValidateTcKimlikAndPhoneAsync(tcKimlik.Value, phoneNumber.Value);
        
        if (!isValid || user == null)
        {
            throw new FshException(
                "TC Kimlik No ve Telefon numarası eşleşmiyor. Lütfen bilgilerinizi kontrol edip tekrar deneyiniz.");
        }

        // Generate and send SMS code
        var smsCode = GenerateSmsCode();
        await _smsService.SendSmsAsync(phoneNumber.Value, $"Şifre sıfırlama kodunuz: {smsCode}");
        
        return "SMS kodu telefon numaranıza gönderildi.";
    }
        
    private static string GenerateSmsCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}