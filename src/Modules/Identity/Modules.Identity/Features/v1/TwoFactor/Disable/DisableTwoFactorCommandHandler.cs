using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.v1.TwoFactor;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.AspNetCore.Identity;

namespace FSH.Modules.Identity.Features.v1.TwoFactor.Disable;

public sealed class DisableTwoFactorCommandHandler
    : ICommandHandler<DisableTwoFactorCommand, bool>
{
    private readonly UserManager<FshUser> _userManager;
    private readonly ICurrentUser _currentUser;

    public DisableTwoFactorCommandHandler(UserManager<FshUser> userManager, ICurrentUser currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async ValueTask<bool> Handle(
        DisableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.GetUserId().ToString();
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.");

        // Require current password so a stolen access token alone can't downgrade
        // account security.
        if (!await _userManager.CheckPasswordAsync(user, command.CurrentPassword))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        return true;
    }
}
