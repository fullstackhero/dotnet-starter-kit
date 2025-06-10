using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Handles phone number update requests for user profiles.
/// SECURITY: Requires verification BEFORE updating phone number.
/// Flow: 1) Validate → 2) Send verification SMS → 3) User verifies → 4) Phone updated
/// </summary>
public sealed class UpdatePhoneCommandHandler : IRequestHandler<UpdatePhoneCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IVerificationService _verificationService;
    private readonly ILogger<UpdatePhoneCommandHandler> _logger;

    public UpdatePhoneCommandHandler(
        IUserRepository userRepository,
        IVerificationService verificationService,
        ILogger<UpdatePhoneCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(UpdatePhoneCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing phone update request for user: {UserId}", request.UserId);

        // Validate phone format
        var phoneResult = PhoneNumber.Create(request.NewPhoneNumber);
        if (!phoneResult.IsSuccess)
        {
            _logger.LogWarning("Invalid phone format: {Phone}", request.NewPhoneNumber);
            throw new FshException($"Invalid phone format: {phoneResult.Error}");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Phone update attempted for non-existent user: {UserId}", request.UserId);
            throw new FshException("User not found");
        }

        // Check if phone is already in use by another user
        if (await _userRepository.PhoneExistsAsync(request.NewPhoneNumber, request.UserId))
        {
            _logger.LogWarning(
                "Phone already exists: {Phone}, requested by user: {UserId}",
                request.NewPhoneNumber,
                request.UserId);

            throw new FshException("Phone number is already in use");
        }

        // SECURITY: Send verification code to NEW phone BEFORE updating
        try
        {
            // Generate and store verification token
            var verificationToken = await _verificationService.GeneratePhoneVerificationTokenAsync(request.NewPhoneNumber);
            
            // Send verification SMS to the NEW phone number
            await _verificationService.SendVerificationSmsAsync(request.NewPhoneNumber, verificationToken);

            _logger.LogInformation(
                "Phone verification sent for user: {UserId}, new phone: {NewPhone}",
                request.UserId,
                request.NewPhoneNumber);

            return $"Verification SMS sent to {request.NewPhoneNumber}. Please check your messages and verify to complete phone update.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification SMS for user: {UserId}", request.UserId);
            throw new FshException("Failed to send verification SMS. Please try again.");
        }
        
        // NOTE: Actual phone update will happen in VerifyPhoneUpdateCommand
        // after user provides the verification code from their SMS
    }
} 