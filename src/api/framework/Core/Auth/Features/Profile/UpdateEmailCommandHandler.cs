using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Exceptions;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Handles email update requests for user profiles.
/// SECURITY: Requires verification BEFORE updating email address.
/// Flow: 1) Validate → 2) Send verification → 3) User verifies → 4) Email updated
/// </summary>
public sealed class UpdateEmailCommandHandler : IRequestHandler<UpdateEmailCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IVerificationService _verificationService;
    private readonly ILogger<UpdateEmailCommandHandler> _logger;

    public UpdateEmailCommandHandler(
        IUserRepository userRepository,
        IVerificationService verificationService,
        ILogger<UpdateEmailCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> Handle(UpdateEmailCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Processing email update request for user: {UserId}", request.UserId);

        // Validate email format
        var emailResult = Email.Create(request.NewEmail);
        if (!emailResult.IsSuccess)
        {
            _logger.LogWarning("Invalid email format: {Email}", request.NewEmail);
            throw new FshException($"Invalid email format: {emailResult.Error}");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Email update attempted for non-existent user: {UserId}", request.UserId);
            throw new FshException("User not found");
        }

        // Check if email is already in use by another user
        if (await _userRepository.EmailExistsAsync(request.NewEmail, request.UserId))
        {
            _logger.LogWarning(
                "Email already exists: {Email}, requested by user: {UserId}",
                request.NewEmail,
                request.UserId);

            throw new FshException("Email address is already in use");
        }

        // SECURITY: Send verification code to NEW email BEFORE updating
        try
        {
            // Generate and store verification token
            var verificationToken = await _verificationService.GenerateEmailVerificationTokenAsync(request.NewEmail);
            
            // Send verification email to the NEW email address
            await _verificationService.SendVerificationEmailAsync(request.NewEmail, verificationToken);

            _logger.LogInformation(
                "Email verification sent for user: {UserId}, new email: {NewEmail}",
                request.UserId,
                request.NewEmail);

            return $"Verification email sent to {request.NewEmail}. Please check your inbox and verify to complete email update.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email for user: {UserId}", request.UserId);
            throw new FshException("Failed to send verification email. Please try again.");
        }
        
        // NOTE: Actual email update will happen in VerifyEmailUpdateCommand
        // after user provides the verification code from their email
    }
} 