using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Models;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Core.Auth.Features.VerifyRegistration;

public class VerifyRegistrationCommandHandler : IRequestHandler<VerifyRegistrationCommand, VerifyRegistrationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<VerifyRegistrationCommandHandler> _logger;

    public VerifyRegistrationCommandHandler(
        IUserRepository userRepository,
        ICacheService cacheService,
        ILogger<VerifyRegistrationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<VerifyRegistrationResponse> Handle(VerifyRegistrationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get pending registration from cache
            var cacheKey = $"pending_reg_{request.PhoneNumber}";
            _logger.LogInformation("Verifying OTP for phone: {PhoneNumber}, Cache Key: {CacheKey}, Provided OTP: {OtpCode}", 
                request.PhoneNumber, cacheKey, request.OtpCode);
                
            var pendingRegistration = await _cacheService.GetAsync<PendingRegistration>(cacheKey, cancellationToken);

            if (pendingRegistration == null)
            {
                _logger.LogWarning("Pending registration not found in cache for phone: {PhoneNumber}, Cache Key: {CacheKey}", 
                    request.PhoneNumber, cacheKey);
                return new VerifyRegistrationResponse(false, "Doğrulama kodu bulunamadı veya süresi dolmuş. Lütfen tekrar kayıt olmayı deneyiniz.");
            }

            _logger.LogInformation("Found pending registration. Stored OTP: {StoredOtp}, Provided OTP: {ProvidedOtp}, Attempts: {Attempts}, IsExpired: {IsExpired}", 
                pendingRegistration.OtpCode, request.OtpCode, pendingRegistration.Attempts, pendingRegistration.IsExpired);

            // 2. Validate OTP
            if (!pendingRegistration.ValidateOtp(request.OtpCode))
            {
                // Update cache with increased attempt count
                await _cacheService.SetAsync(cacheKey, pendingRegistration, TimeSpan.FromMinutes(15), cancellationToken);

                if (pendingRegistration.HasExceededMaxAttempts)
                {
                    await _cacheService.RemoveAsync(cacheKey, cancellationToken);
                    return new VerifyRegistrationResponse(false, "Çok fazla hatalı deneme yaptınız. Lütfen tekrar kayıt olmayı deneyiniz.");
                }

                return new VerifyRegistrationResponse(false, $"Doğrulama kodu hatalı. Kalan deneme hakkınız: {3 - pendingRegistration.Attempts}");
            }

            // 3. Create user from pending registration
            var data = pendingRegistration.RegistrationData;
            var userResult = AppUser.Create(
                data.Email,
                data.Username,
                data.PhoneNumber,
                data.Tckn,
                data.FirstName,
                data.LastName,
                data.ProfessionId,
                data.BirthDate.GetValueOrDefault(DateTime.Today.AddYears(-18)),
                data.MarketingConsent,
                data.ElectronicCommunicationConsent,
                data.MembershipAgreementConsent,
                pendingRegistration.RegistrationIp
            );

            if (!userResult.IsSuccess)
            {
                return new VerifyRegistrationResponse(false, $"Kullanıcı oluşturulamadı: {userResult.Error}");
            }

            // Set password and finalize user
            var user = userResult.Value.SetPassword(data.Password);

            // 4. Save user to database
            var userId = await _userRepository.CreateUserAsync(user);

            // 5. Remove from cache
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation("User registration completed successfully for phone: {PhoneNumber}, UserId: {UserId}", 
                request.PhoneNumber, userId);

            return new VerifyRegistrationResponse(
                true, 
                "Kayıt işleminiz başarıyla tamamlandı. Artık giriş yapabilirsiniz.",
                userId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying registration for phone: {PhoneNumber}", request.PhoneNumber);
            return new VerifyRegistrationResponse(false, "Doğrulama işlemi sırasında bir hata oluştu. Lütfen tekrar deneyiniz.");
        }
    }
} 