using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing password change for user: {UserId}", request.UserId);

        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Password change attempted for non-existent user: {UserId}", request.UserId);
            throw new FshException("User not found");
        }

        // Validate current password
        var isCurrentPasswordValid = await _userRepository.ValidateCurrentPasswordAsync(request.UserId, request.CurrentPassword)
            .ConfigureAwait(false);

        if (!isCurrentPasswordValid)
        {
            _logger.LogWarning("Incorrect current password provided for user: {UserId}", request.UserId);
            throw new FshException("Current password is incorrect");
        }
        
        // Set new password using domain logic
        user.SetPassword(request.NewPassword);

        // Update password in repository  
        await _userRepository.UpdatePasswordAsync(user.Id, user.PasswordHash)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Password successfully changed for user: {UserId}, {Email}, {Username}",
            request.UserId,
            user.Email.Value,
            user.Username);

        return "Password changed successfully";
    }
} 