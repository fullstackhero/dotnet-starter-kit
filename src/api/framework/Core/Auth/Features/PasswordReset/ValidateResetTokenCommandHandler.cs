using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed class ValidateResetTokenCommandHandler : IRequestHandler<ValidateResetTokenCommand, Result<ValidateResetTokenResponse>>
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<ValidateResetTokenCommandHandler> _logger;

    public ValidateResetTokenCommandHandler(
        IPasswordResetService passwordResetService,
        ILogger<ValidateResetTokenCommandHandler> logger)
    {
        _passwordResetService = passwordResetService ?? throw new ArgumentNullException(nameof(passwordResetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ValidateResetTokenResponse>> Handle(ValidateResetTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result<ValidateResetTokenResponse>.Success(new ValidateResetTokenResponse
            {
                IsValid = false,
                Message = "Token gereklidir."
            });
        }

        try
        {
            var isValid = await _passwordResetService.ValidateResetTokenAsync(request.Token);
            
            if (isValid)
            {
                _logger.LogInformation("Reset token validation successful for token: {Token}", request.Token[..8] + "...");
                
                return Result<ValidateResetTokenResponse>.Success(new ValidateResetTokenResponse
                {
                    IsValid = true,
                    Message = "Token geçerli."
                });
            }
            else
            {
                _logger.LogWarning("Reset token validation failed for token: {Token}", request.Token[..8] + "...");
                
                return Result<ValidateResetTokenResponse>.Success(new ValidateResetTokenResponse
                {
                    IsValid = false,
                    Message = "Token geçersiz veya süresi dolmuş."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token: {Token}", request.Token[..8] + "...");
            
            return Result<ValidateResetTokenResponse>.Failure("Token doğrulanırken bir hata oluştu.");
        }
    }
} 