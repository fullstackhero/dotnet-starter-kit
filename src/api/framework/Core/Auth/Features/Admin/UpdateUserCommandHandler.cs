using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using EmailVO = FSH.Framework.Core.Auth.Domain.ValueObjects.Email;
using PhoneNumberVO = FSH.Framework.Core.Auth.Domain.ValueObjects.PhoneNumber;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace FSH.Framework.Core.Auth.Features.Admin;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UpdateUserResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UpdateUserResult>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting user update process for user ID: {UserId}", request.UserId);

            // Get existing user
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID {UserId}", request.UserId);
                return Result<UpdateUserResult>.Failure($"User with ID {request.UserId} not found");
            }

            // Check if email is being changed and if it's already in use
            if (!string.Equals(user.Email.Value, request.Email.Value, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userRepository.EmailExistsAsync(request.Email.Value))
                {
                    _logger.LogWarning("Email {Email} is already in use", request.Email.Value);
                    return Result<UpdateUserResult>.Failure($"Email {request.Email.Value} is already in use");
                }
            }

            // Check if username is being changed and if it's already in use
            if (!string.Equals(user.Username, request.Username.Value, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userRepository.UsernameExistsAsync(request.Username.Value))
                {
                    _logger.LogWarning("Username {Username} is already in use", request.Username.Value);
                    return Result<UpdateUserResult>.Failure($"Username {request.Username.Value} is already in use");
                }
            }

            // Update user using builder pattern
            var updatedUser = user
                .WithPasswordHash(user.PasswordHash) // Keep existing password
                .WithEmailVerificationStatus(request.IsEmailVerified)
                .WithStatus(request.Status);

            // Save changes
            await _userRepository.UpdateUserAsync(updatedUser);

            _logger.LogInformation("User updated successfully with ID {UserId}", request.UserId);

            return Result<UpdateUserResult>.Success(new UpdateUserResult 
            { 
                UserId = request.UserId, 
                Message = "User updated successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {UserId}", request.UserId);
            return Result<UpdateUserResult>.Failure("An error occurred while updating the user");
        }
    }
}