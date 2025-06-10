using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Handles email address verification and update completion.
/// SECURITY: Only updates email after successful verification of email code.
/// This completes the secure email update process.
/// </summary>
public sealed class VerifyEmailUpdateCommandHandler : IRequestHandler<VerifyEmailUpdateCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IVerificationService _verificationService;
    private readonly ILogger<VerifyEmailUpdateCommandHandler> _logger;

    public VerifyEmailUpdateCommandHandler(
        IUserRepository userRepository,
        IVerificationService verificationService,
        ILogger<VerifyEmailUpdateCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(VerifyEmailUpdateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing email verification for user: {UserId}", request.UserId);

        // Validate email format
        var emailResult = Email.Create(request.NewEmail);
        if (!emailResult.IsSuccess)
        {
            _logger.LogWarning("Invalid email format during verification: {Email}", request.NewEmail);
            throw new FshException($"Invalid email format: {emailResult.Error}");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Email verification attempted for non-existent user: {UserId}", request.UserId);
            throw new FshException("User not found");
        }

        // Verify the email code
        var isCodeValid = await _verificationService.VerifyEmailAsync(request.NewEmail, request.VerificationCode);
        if (!isCodeValid)
        {
            _logger.LogWarning(
                "Invalid verification code provided for user: {UserId}, email: {Email}",
                request.UserId,
                request.NewEmail);
            
            throw new FshException("Invalid verification code");
        }

        // Check if email is already in use by another user (double-check)
        if (await _userRepository.EmailExistsAsync(request.NewEmail, request.UserId))
        {
            _logger.LogWarning(
                "Email already exists during verification: {Email}, requested by user: {UserId}",
                request.NewEmail,
                request.UserId);

            throw new FshException("Email address is already in use");
        }

        // Update the user's email address
        try
        {
            var updatedUser = user.UpdateProfile(email: request.NewEmail);
            if (!updatedUser.IsSuccess)
            {
                _logger.LogError("Failed to update user email: {Error}", updatedUser.Error);
                throw new FshException($"Failed to update email: {updatedUser.Error}");
            }

            // Update in database
            await _userRepository.UpdateUserAsync(updatedUser.Value!);

            _logger.LogInformation(
                "Email address successfully updated for user: {UserId}, new email: {NewEmail}",
                request.UserId,
                request.NewEmail);

            return "Email address updated successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update email address for user: {UserId}", request.UserId);
            throw new FshException("Failed to update email address. Please try again.");
        }
    }
} 