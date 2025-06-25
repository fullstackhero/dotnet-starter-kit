using MediatR;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Handler for verifying phone update with verification code.
/// SECURITY: Validates code before updating phone in database.
/// </summary>
public sealed class VerifyPhoneUpdateCommandHandler : IRequestHandler<VerifyPhoneUpdateCommand, string>
{
    private readonly IUserRepository _userRepository;

    public VerifyPhoneUpdateCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<string> Handle(VerifyPhoneUpdateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate phone format
        var phoneResult = PhoneNumber.Create(request.NewPhoneNumber);
        if (!phoneResult.IsSuccess)
        {
            throw new FshException("Geçersiz telefon numarası formatı");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            throw new FshException("Kullanıcı bulunamadı");
        }

        // Check if phone is already in use by another user
        var phoneExists = await _userRepository.PhoneExistsAsync(request.NewPhoneNumber, request.UserId)
            .ConfigureAwait(false);

        if (phoneExists)
        {
            throw new FshException("Bu telefon numarası zaten kullanılıyor");
        }

        // Verify the code and update phone
        var isVerified = await _userRepository.VerifyPhoneUpdateAsync(request.UserId, request.VerificationCode)
            .ConfigureAwait(false);

        if (!isVerified)
        {
            throw new FshException("Geçersiz doğrulama kodu");
        }

        return "Telefon numaranız başarıyla güncellendi";
    }
} 