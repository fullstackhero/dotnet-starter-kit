using MediatR;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.Profile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, string>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<string> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            throw new FshException("User not found");
        }

        // Check if username is being updated and if it's available
        if (!string.IsNullOrEmpty(request.Username) && 
            request.Username != user.Username)
        {
            var usernameExists = await _userRepository.UsernameExistsAsync(request.Username, request.UserId)
                .ConfigureAwait(false);

            if (usernameExists)
            {
                throw new FshException("Bu kullanıcı adı zaten kullanılıyor. Lütfen farklı bir kullanıcı adı seçiniz.");
            }
        }

        await _userRepository.UpdateProfileAsync(
            request.UserId, 
            request.Username, 
            request.Profession)
            .ConfigureAwait(false);

        return "Profil bilgileriniz başarıyla güncellendi.";
    }
} 