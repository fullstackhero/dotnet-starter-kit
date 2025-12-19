using FSH.Framework.Shared.Identity.Claims;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ChangePassword;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using FSH.Modules.Identity.Features.v1.Users;

namespace FSH.Modules.Identity.Features.v1.Users.ChangePassword;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, string>
{
    private readonly IUserService _userService;
    private readonly IPasswordHistoryService _passwordHistoryService;
    private readonly IPasswordExpiryService _passwordExpiryService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<FshUser> _userManager;

    public ChangePasswordCommandHandler(
        IUserService userService,
        IPasswordHistoryService passwordHistoryService,
        IPasswordExpiryService passwordExpiryService,
        IHttpContextAccessor httpContextAccessor,
        UserManager<FshUser> userManager)
    {
        _userService = userService;
        _passwordHistoryService = passwordHistoryService;
        _passwordExpiryService = passwordExpiryService;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async ValueTask<string> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userId = _httpContextAccessor.HttpContext?.User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        await _userService.ChangePasswordAsync(command.Password, command.NewPassword, command.ConfirmNewPassword, userId).ConfigureAwait(false);

        // Record the new password in history
        var updatedUser = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (updatedUser?.PasswordHash is not null)
        {
            await _passwordHistoryService.RecordPasswordChangeAsync(userId, updatedUser.PasswordHash, cancellationToken).ConfigureAwait(false);
            await _passwordHistoryService.CleanupOldPasswordHistoryAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        // Update password change timestamp for expiry tracking
        await _passwordExpiryService.UpdateLastPasswordChangeAsync(userId, cancellationToken).ConfigureAwait(false);

        return "password reset email sent";
    }
}
