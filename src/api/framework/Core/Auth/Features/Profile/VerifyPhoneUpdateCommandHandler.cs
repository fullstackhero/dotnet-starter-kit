using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Handles phone number verification and update completion.
/// SECURITY: Only updates phone after successful verification of SMS code.
/// This completes the secure phone update process.
/// </summary>
public sealed class VerifyPhoneUpdateCommandHandler : IRequestHandler<VerifyPhoneUpdateCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IVerificationService _verificationService;
    private readonly ILogger<VerifyPhoneUpdateCommandHandler> _logger;

    public VerifyPhoneUpdateCommandHandler(
        IUserRepository userRepository,
        IVerificationService verificationService,
        ILogger<VerifyPhoneUpdateCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(VerifyPhoneUpdateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing phone verification for user: {UserId}", request.UserId);

        // Validate phone format
        var phoneResult = PhoneNumber.Create(request.NewPhoneNumber);
        if (!phoneResult.IsSuccess)
        {
            _logger.LogWarning("Invalid phone format during verification: {Phone}", request.NewPhoneNumber);
            throw new FshException($"Invalid phone format: {phoneResult.Error}");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Phone verification attempted for non-existent user: {UserId}", request.UserId);
            throw new FshException("User not found");
        }

        // Verify the SMS code
        var isCodeValid = await _verificationService.VerifyPhoneAsync(request.NewPhoneNumber, request.VerificationCode);
        if (!isCodeValid)
        {
            _logger.LogWarning(
                "Invalid verification code provided for user: {UserId}, phone: {Phone}",
                request.UserId,
                request.NewPhoneNumber);
            
            throw new FshException("Invalid verification code");
        }

        // Check if phone is already in use by another user (double-check)
        if (await _userRepository.PhoneExistsAsync(request.NewPhoneNumber, request.UserId))
        {
            _logger.LogWarning(
                "Phone already exists during verification: {Phone}, requested by user: {UserId}",
                request.NewPhoneNumber,
                request.UserId);

            throw new FshException("Phone number is already in use");
        }

        // Update the user's phone number
        try
        {
            var updatedUser = user.UpdateProfile(phoneNumber: request.NewPhoneNumber);
            if (!updatedUser.IsSuccess)
            {
                _logger.LogError("Failed to update user phone: {Error}", updatedUser.Error);
                throw new FshException($"Failed to update phone: {updatedUser.Error}");
            }

            // Update in database
            await _userRepository.UpdateUserAsync(updatedUser.Value!);

            _logger.LogInformation(
                "Phone number successfully updated for user: {UserId}, new phone: {NewPhone}",
                request.UserId,
                request.NewPhoneNumber);

            return "Phone number updated successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update phone number for user: {UserId}", request.UserId);
            throw new FshException("Failed to update phone number. Please try again.");
        }
    }
} 