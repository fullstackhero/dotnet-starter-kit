using MediatR;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Common.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Handler for verifying email update with verification code.
/// SECURITY: Validates code before updating email in database.
/// </summary>
public sealed class VerifyEmailUpdateCommandHandler : IRequestHandler<VerifyEmailUpdateCommand, string>
{
    private readonly IUserRepository _userRepository;

    public VerifyEmailUpdateCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<string> Handle(VerifyEmailUpdateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate email format
        var emailResult = Email.Create(request.NewEmail);
        if (!emailResult.IsSuccess)
        {
            throw new FshException("Geçersiz email formatı");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            throw new FshException("Kullanıcı bulunamadı");
        }

        // Check if email is already in use by another user
        var emailExists = await _userRepository.EmailExistsAsync(request.NewEmail, request.UserId)
            .ConfigureAwait(false);

        if (emailExists)
        {
            throw new FshException("Bu email adresi zaten kullanılıyor");
        }

        // Verify the code and update email
        var isVerified = await _userRepository.VerifyEmailUpdateAsync(request.UserId, request.VerificationCode)
            .ConfigureAwait(false);

        if (!isVerified)
        {
            throw new FshException("Geçersiz doğrulama kodu");
        }

        return "Email adresiniz başarıyla güncellendi";
    }
}