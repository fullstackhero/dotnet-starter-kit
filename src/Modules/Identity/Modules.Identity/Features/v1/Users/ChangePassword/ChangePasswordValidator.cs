using FluentValidation;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ChangePassword;
using FSH.Modules.Identity.Features.v1.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using FSH.Framework.Shared.Identity.Claims;

namespace FSH.Modules.Identity.Features.v1.Users.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    private readonly UserManager<FshUser> _userManager;
    private readonly IPasswordHistoryService _passwordHistoryService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChangePasswordValidator(
        UserManager<FshUser> userManager,
        IPasswordHistoryService passwordHistoryService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _passwordHistoryService = passwordHistoryService;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(p => p.Password)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(p => p.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .NotEqual(p => p.Password)
            .WithMessage("New password must be different from the current password.");

        RuleFor(p => p.ConfirmNewPassword)
            .Equal(p => p.NewPassword)
            .WithMessage("Passwords do not match.");

        RuleFor(p => p.NewPassword)
            .MustAsync(async (newPassword, cancellationToken) =>
            {
                if (!_passwordHistoryService.IsEnabled())
                {
                    return true;
                }

                var userId = _httpContextAccessor.HttpContext?.User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return true;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return true;
                }

                var newPasswordHash = _userManager.PasswordHasher.HashPassword(user, newPassword);
                var isPasswordReused = await _passwordHistoryService.IsPasswordUsedBeforeAsync(
                    userId, newPasswordHash, cancellationToken);

                return !isPasswordReused;
            })
            .WithMessage(ctx => 
            {
                var preventReuse = _passwordHistoryService.GetPasswordsToPreventReuse();
                return $"You cannot reuse one of your last {preventReuse} passwords.";
            });
    }
}