using FSH.Framework.Core.Auth.Models;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Auth.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;

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

            // Generate unique username automatically
            var username = await GenerateUniqueUsernameAsync(request.LastName);

            var usernameExists = await _userRepository.UsernameExistsAsync(username);
            if (usernameExists)
            {
                // If generated username exists, try with more numbers
                username = await GenerateUniqueUsernameAsync(request.LastName, true);
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
                Username = username, // Use generated username
                PhoneNumber = request.PhoneNumber,
                Tckn = request.Tckn,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ProfessionId = request.ProfessionId,
                BirthDate = request.BirthDate,
                MarketingConsent = request.MarketingConsent,
                ElectronicCommunicationConsent = request.ElectronicCommunicationConsent,
                MembershipAgreementConsent = request.MembershipAgreementConsent
            };

            // 3. Create pending registration with OTP
            var pendingRegistration = PendingRegistration.Create(registrationData, request.RegistrationIp, request.DeviceInfo);

            // 4. Store in cache
            var cacheKey = $"pending_reg_{request.PhoneNumber}";
            _logger.LogInformation("Storing pending registration in cache. Phone: {PhoneNumber}, Cache Key: {CacheKey}, OTP: {OtpCode}", 
                request.PhoneNumber, cacheKey, pendingRegistration.OtpCode);
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

    private async Task<string> GenerateUniqueUsernameAsync(string lastName, bool useMoreNumbers = false)
    {
        // Clean lastName and make it lowercase
        var baseName = lastName.ToUpperInvariant()
            .Replace("ç", "c", StringComparison.OrdinalIgnoreCase)
            .Replace("ğ", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("ı", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ö", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ş", "s", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "u", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "", StringComparison.Ordinal);

        // Remove any non-alphanumeric characters
        baseName = new string(baseName.Where(char.IsLetterOrDigit).ToArray());

        // Ensure it's not too long
        if (baseName.Length > 15)
        {
            baseName = baseName.Substring(0, 15);
        }

        var random = new Random();
        string username;
        int attempts = 0;
        int maxAttempts = useMoreNumbers ? 100 : 10;

        do
        {
            var numberLength = useMoreNumbers ? 4 : 3;
            var randomBytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            int randomNumber = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % (int)Math.Pow(10, numberLength) / (int)Math.Pow(10, numberLength - 1);
            username = $"{baseName}{randomNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            attempts++;

            var exists = await _userRepository.UsernameExistsAsync(username);
            if (!exists)
            {
                return username;
            }
        } while (attempts < maxAttempts);

        // If all attempts failed, use timestamp
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"{baseName}{timestamp.Substring(timestamp.Length - 6)}";
    }
}