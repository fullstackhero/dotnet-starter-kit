using FluentValidation;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ChangePassword;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Users.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    private readonly IPasswordHistoryService _passwordHistoryService;
    private readonly ICurrentUser _currentUser;

    public ChangePasswordValidator(
        IPasswordHistoryService passwordHistoryService,
        ICurrentUser currentUser,
        IStringLocalizer<SharedResources> localizer)
    {
        _passwordHistoryService = passwordHistoryService;
        _currentUser = currentUser;

        RuleFor(p => p.Password)
            .NotEmpty()
            .WithMessage(_ => localizer["Validation.CurrentPasswordRequired"]);

        RuleFor(p => p.NewPassword)
            .NotEmpty()
            .WithMessage(_ => localizer["Validation.NewPasswordRequired"])
            .NotEqual(p => p.Password)
            .WithMessage(_ => localizer["Validation.NewPasswordMustDiffer"])
            .MustAsync(NotBeInPasswordHistoryAsync)
            .WithMessage(_ => localizer["Validation.PasswordRecentlyUsed"]);

        RuleFor(p => p.ConfirmNewPassword)
            .Equal(p => p.NewPassword)
            .WithMessage(_ => localizer["Validation.PasswordsDoNotMatch"]);
    }

    private async Task<bool> NotBeInPasswordHistoryAsync(string newPassword, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated())
        {
            return true; // Let other validation handle unauthorized access
        }

        var userId = _currentUser.GetUserId().ToString();

        // Check if password is in history
        var isInHistory = await _passwordHistoryService.IsPasswordInHistoryAsync(userId, newPassword, cancellationToken);
        return !isInHistory; // Return true if NOT in history (validation passes)
    }
}