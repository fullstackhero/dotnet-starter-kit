using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using FSH.Framework.Core.Auth;
using FSH.Framework.Core.Auth.Dtos;
using FSH.Framework.Core.Auth.Features.Identity;
using FSH.Framework.Core.Auth.Features.Login;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using FSH.Framework.Core.Auth.Features.Profile;
using FSH.Framework.Core.Auth.Features.Register;
using FSH.Framework.Core.Auth.Features.Token.Generate;
using FSH.Framework.Core.Auth.Features.Token.Refresh;
using FSH.Framework.Core.Auth.Features.User;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Starter.WebApi.Contracts.Auth;
using FSH.Starter.WebApi.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Host;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("test")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Test()
    {
        return Ok(new { Message = "Auth API is working!" });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        // Clean Architecture: Map presentation DTO to domain command
        var tcknResult = Tckn.Create(request.Tckn);
        if (!tcknResult.IsSuccess)
        {
            return BadRequest(ApiResponse.FailureResult($"TC Kimlik No hatası: {tcknResult.Error}"));
        }

        var passwordResult = Password.Create(request.Password);
        if (!passwordResult.IsSuccess)
        {
            return BadRequest(ApiResponse.FailureResult($"Şifre hatası: {passwordResult.Error}"));
        }

        var command = new LoginCommand
        {
            Tckn = tcknResult.Value!,
            Password = passwordResult.Value!
        };

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<LoginResponseDto>.SuccessResult(result.Value!));
        }
        else
        {
            return Ok(ApiResponse<LoginResponseDto>.FailureResult(result.Error ?? "Unknown error"));
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        // Clean Architecture: Map presentation DTO to domain command
        var command = new RegisterCommand
        {
            Email = request.Email,
            Username = request.Username,
            PhoneNumber = request.PhoneNumber,
            Tckn = request.Tckn,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Profession = request.Profession,
            BirthDate = request.BirthDate
        };

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<RegisterResponseDto>.SuccessResult(result.Value!));
        }
        else
        {
            return Ok(ApiResponse<RegisterResponseDto>.FailureResult(result.Error ?? "Unknown error"));
        }
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FSH.Framework.Core.Auth.Features.Token.Generate.TokenGenerationResult>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GenerateTokenAsync([FromBody] GenerateTokenCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<FSH.Framework.Core.Auth.Features.Token.Generate.TokenGenerationResult>.SuccessResult(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<TokenResponseDto>.SuccessResult(result));
    }

    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(400)]
    public IActionResult GetPermissions()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        return Ok(roles);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] FSH.Framework.Core.Auth.Features.PasswordReset.ForgotPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    [HttpPost("validate-tc-phone")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ValidateTcPhoneAsync([FromBody] FSH.Framework.Core.Auth.Features.PasswordReset.ValidateTcPhoneCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] FSH.Framework.Core.Auth.Features.PasswordReset.ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] FSH.Framework.Core.Auth.Features.PasswordReset.ChangePasswordCommand command)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Ok(ApiResponse<string>.FailureResult("Unable to determine current user"));
        }

        command.UserId = userId;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetProfileAsync()
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Ok(ApiResponse<UserDetailDto>.FailureResult("Unable to determine current user"));
        }

        var query = new GetUserProfileQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<UserDetailDto>.SuccessResult(result));
    }

    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileCommand command)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Ok(ApiResponse<string>.FailureResult("Unable to determine current user"));
        }

        command.UserId = userId;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    /// <summary>
    /// Initiates email update process by sending verification code to new email.
    /// SECURITY: Email is NOT updated until verification is completed.
    /// TODO: Integrate with email service to send verification codes.
    /// </summary>
    [HttpPut("profile/email")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateEmailAsync([FromBody] UpdateEmailCommand command)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Ok(ApiResponse<string>.FailureResult("Unable to determine current user"));
        }

        command.UserId = userId;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    /// <summary>
    /// Completes email update by verifying email code.
    /// SECURITY: Only updates email address after successful email verification.
    /// </summary>
    [HttpPost("profile/verify-email")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmailUpdateAsync([FromBody] VerifyEmailUpdateCommand command)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Ok(ApiResponse<string>.FailureResult("Unable to determine current user"));
        }

        command.UserId = userId;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    /// <summary>
    /// Initiates phone update process by sending verification code to new phone.
    /// SECURITY: Phone is NOT updated until verification is completed.
    /// TODO: Integrate with SMS service to send verification codes.
    /// </summary>
    [HttpPut("profile/phone")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePhoneAsync([FromBody] UpdatePhoneCommand command)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Ok(ApiResponse<string>.FailureResult("Unable to determine current user"));
        }

        command.UserId = userId;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    /// <summary>
    /// Completes phone update by verifying SMS code.
    /// SECURITY: Only updates phone number after successful SMS verification.
    /// </summary>
    [HttpPost("profile/verify-phone")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyPhoneUpdateAsync([FromBody] VerifyPhoneUpdateCommand command)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Ok(ApiResponse<string>.FailureResult("Unable to determine current user"));
        }

        command.UserId = userId;
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    [HttpPost("test-mernis")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TestMernisResult>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestMernisAsync([FromBody] FSH.Framework.Core.Auth.Features.Identity.TestMernisRequest request)
    {
        var result = await _mediator.Send(request);
        return Ok(ApiResponse<TestMernisResult>.SuccessResult(result));
    }

    [HttpPost("debug-user")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> DebugUserAsync([FromBody] DebugUserRequest request)
    {
        try
        {
            // Use the repository directly to check what's in the database
            var userRepo = HttpContext.RequestServices.GetRequiredService<FSH.Framework.Core.Auth.Repositories.IUserRepository>();
            
            // Try to get user by TCKN first
            var user = await userRepo.GetByTcknAsync(Tckn.CreateUnsafe(request.Tckn));
            
            if (user == null)
            {
                return Ok(new { 
                    found = false, 
                    message = "No user found with this TCKN",
                    tckn = request.Tckn
                });
            }

            // Test password validation
            var (isValid, userFromValidation) = await userRepo.ValidatePasswordAndGetByTcknAsync(request.Tckn, request.Password);

            return Ok(new {
                found = true,
                user = new {
                    id = user.Id,
                    email = user.Email.Value,
                    username = user.Username,
                    phoneNumber = user.PhoneNumber.Value,
                    tckn = user.Tckn.Value,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    hasPasswordHash = !string.IsNullOrEmpty(user.PasswordHash),
                    passwordHashLength = user.PasswordHash?.Length ?? 0,
                    status = user.Status,
                    isEmailVerified = user.IsEmailVerified,
                    isPhoneVerified = user.IsPhoneVerified,
                    isIdentityVerified = user.IsIdentityVerified
                },
                passwordValidation = new {
                    isValid = isValid,
                    foundUserFromValidation = userFromValidation != null
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new { 
                error = true, 
                message = ex.Message,
                type = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// Development: Get current verification tokens for testing
    /// </summary>
    [HttpGet("debug-tokens")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetDebugTokens()
    {
        try
        {
            var verificationService = HttpContext.RequestServices.GetRequiredService<IVerificationService>();
            
            // Note: Bu sadece development için. Production'da kaldırılmalı.
            return Ok(new {
                message = "Check console logs for email verification tokens",
                instruction = "Look for '🔧 DEV: Email verification token' in logs",
                currentTime = DateTime.UtcNow,
                emailTokenExpiryMinutes = 60,
                phoneTokenExpiryMinutes = 15
            });
        }
        catch (Exception ex)
        {
            return Ok(new { 
                error = true, 
                message = ex.Message,
                type = ex.GetType().Name
            });
        }
    }
}

public sealed class DebugUserRequest
{
    public string Tckn { get; init; } = default!;
    public string Password { get; init; } = default!;
}
