using FSH.Framework.Core.Auth.Models;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Auth.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Core.Auth.Features.RegisterRequest;

public class RegisterRequestCommandHandler : IRequestHandler<RegisterRequestCommand, RegisterRequestResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly ISmsService _smsService;
    private readonly ILogger<RegisterRequestCommandHandler> _logger;

    public RegisterRequestCommandHandler(
        IUserRepository userRepository,
        ICacheService cacheService,
        ISmsService smsService,
        ILogger<RegisterRequestCommandHandler> logger)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<RegisterRequestResponse> Handle(RegisterRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Check if user already exists
            var emailExists = await _userRepository.EmailExistsAsync(request.Email);
            if (emailExists)
            {
                return new RegisterRequestResponse(false, "Bu email adresi zaten kullanılmaktadır.");
            }

            var usernameExists = await _userRepository.UsernameExistsAsync(request.Username);
            if (usernameExists)
            {
                return new RegisterRequestResponse(false, "Bu kullanıcı adı zaten kullanılmaktadır.");
            }

            var phoneExists = await _userRepository.PhoneExistsAsync(request.PhoneNumber);
            if (phoneExists)
            {
                return new RegisterRequestResponse(false, "Bu telefon numarası zaten kullanılmaktadır.");
            }

            var tcknExists = await _userRepository.TcKimlikExistsAsync(request.Tckn);
            if (tcknExists)
            {
                return new RegisterRequestResponse(false, "Bu TC kimlik numarası zaten kullanılmaktadır.");
            }

            // 2. Create pending registration data
            var registrationData = new PendingRegistrationData
            {
                Email = request.Email,
                Username = request.Username,
                PhoneNumber = request.PhoneNumber,
                Tckn = request.Tckn,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ProfessionId = request.ProfessionId,
                BirthDate = request.BirthDate
            };

            // 3. Create pending registration with OTP
            var pendingRegistration = PendingRegistration.Create(registrationData, request.RegistrationIp, request.DeviceInfo);

            // 4. Store in cache
            var cacheKey = $"pending_reg_{request.PhoneNumber}";
            await _cacheService.SetAsync(cacheKey, pendingRegistration, TimeSpan.FromMinutes(15), cancellationToken);

            // 5. Send SMS OTP
            var smsSent = await _smsService.SendSmsCodeAsync(request.PhoneNumber, pendingRegistration.OtpCode);
            
            if (!smsSent)
            {
                await _cacheService.RemoveAsync(cacheKey, cancellationToken);
                return new RegisterRequestResponse(false, "SMS gönderilirken bir hata oluştu. Lütfen tekrar deneyiniz.");
            }

            _logger.LogInformation("Registration request created for phone: {PhoneNumber}", request.PhoneNumber);

            return new RegisterRequestResponse(
                true, 
                "Doğrulama kodu telefon numaranıza gönderildi. Lütfen 15 dakika içinde doğrulayınız.",
                request.PhoneNumber
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling register request for phone: {PhoneNumber}", request.PhoneNumber);
            return new RegisterRequestResponse(false, "Kayıt işlemi sırasında bir hata oluştu. Lütfen tekrar deneyiniz.");
        }
    }
} 