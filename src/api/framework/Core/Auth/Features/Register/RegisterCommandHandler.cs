using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityVerificationService _identityVerificationService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IIdentityVerificationService identityVerificationService,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _identityVerificationService = identityVerificationService;
        _logger = logger;
    }

    public async Task<Result<RegisterResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting registration process for email: {Email}", request.Email);

            // Validate TCKN
            if (!Tckn.IsValid(request.Tckn))
            {
                _logger.LogWarning("Invalid TCKN provided: {Tckn}", request.Tckn);
                return Result<RegisterResponseDto>.Failure("Invalid TCKN");
            }

            // Check if user with same email, username or TCKN already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                _logger.LogWarning("User with email {Email} already exists", request.Email);
                return Result<RegisterResponseDto>.Failure($"User with email {request.Email} already exists");
            }

            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                _logger.LogWarning("User with username {Username} already exists", request.Username);
                return Result<RegisterResponseDto>.Failure($"User with username {request.Username} already exists");
            }

            if (await _userRepository.TcKimlikExistsAsync(request.Tckn))
            {
                _logger.LogWarning("User with TCKN already exists");
                return Result<RegisterResponseDto>.Failure("User with this TCKN already exists");
            }

            // Verify identity with external service
            var identityVerificationResult = await _identityVerificationService.VerifyIdentityAsync(
                request.Tckn,
                request.FirstName,
                request.LastName,
                request.BirthDate?.Year ?? DateTime.MinValue.Year);

            if (!identityVerificationResult)
            {
                _logger.LogWarning("Identity verification failed for TCKN");
                return Result<RegisterResponseDto>.Failure("Identity verification failed");
            }

            // Create domain user entity
            var userResult = AppUser.Create(
                email: request.Email,
                username: request.Username,
                phoneNumber: request.PhoneNumber,
                tckn: request.Tckn,
                firstName: request.FirstName,
                lastName: request.LastName,
                profession: request.Profession ?? string.Empty,
                birthDate: request.BirthDate ?? DateTime.MinValue
            );

            if (!userResult.IsSuccess)
            {
                _logger.LogWarning("Failed to create user: {Error}", userResult.Error);
                return Result<RegisterResponseDto>.Failure(userResult.Error!);
            }

            var user = userResult.Value;

            // Set password - SetPassword returns a new instance with hashed password
            user = user.SetPassword(request.Password);

            // Save user
            var userId = await _userRepository.CreateUserAsync(user);

            // Assign default base_user role to new users
            await _userRepository.AssignRoleAsync(userId, "base_user");

            _logger.LogInformation("User registered successfully with ID {UserId}", userId);

            return Result<RegisterResponseDto>.Success(new RegisterResponseDto 
            { 
                UserId = userId,
                Email = request.Email,
                Username = request.Username,
                Message = "User registered successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return Result<RegisterResponseDto>.Failure("An error occurred while registering the user");
        }
    }
} 