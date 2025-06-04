using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.Profile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing profile update for user: {UserId}", request.UserId);

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Profile update attempted for non-existent user: {UserId}", request.UserId);
            throw new FshException("User not found");
        }

        // Username uniqueness check
        if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
        {
            var usernameExists = await _userRepository.UsernameExistsAsync(request.Username, request.UserId)
                .ConfigureAwait(false);

            if (usernameExists)
            {
                _logger.LogWarning(
                    "Username already exists: {Username}, requested by user: {UserId}",
                    request.Username,
                    request.UserId);

                throw new FshException("Username already exists");
            }
        }

        await _userRepository.UpdateProfileAsync(request.UserId, request.Username, request.Profession)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Profile successfully updated for user: {UserId}, {Email}, {Username}",
            request.UserId,
            user.Email.Value,
            user.Username);

        return "Profile updated successfully";
    }
} 