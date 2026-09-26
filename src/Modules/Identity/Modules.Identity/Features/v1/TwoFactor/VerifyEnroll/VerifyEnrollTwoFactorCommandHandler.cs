using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.v1.TwoFactor;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Localization;
using Mediator;
using Microsoft.AspNetCore.Identity;

namespace FSH.Modules.Identity.Features.v1.TwoFactor.VerifyEnroll;

public sealed class VerifyEnrollTwoFactorCommandHandler
    : ICommandHandler<VerifyEnrollTwoFactorCommand, bool>
{
    private readonly UserManager<FshUser> _userManager;
    private readonly ICurrentUser _currentUser;

    public VerifyEnrollTwoFactorCommandHandler(UserManager<FshUser> userManager, ICurrentUser currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async ValueTask<bool> Handle(
        VerifyEnrollTwoFactorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.GetUserId().ToString();
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.")
            {
                MessageKey = "Identity.UserNotFoundById",
                MessageArgs = [userId],
                ResourceSource = typeof(IdentityResources),
            };

        var sanitized = command.Code.Replace(" ", string.Empty, StringComparison.Ordinal);
        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            sanitized);

        if (!valid)
        {
            throw new CustomException(
                "The authenticator code is invalid.",
                errors: null,
                System.Net.HttpStatusCode.BadRequest)
            {
                MessageKey = "Identity.AuthenticatorCodeInvalid",
                ResourceSource = typeof(IdentityResources),
            };
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        return true;
    }
}
