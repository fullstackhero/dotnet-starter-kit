using System.Text.Encodings.Web;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.TwoFactor;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Features.v1.TwoFactor.Enroll;

public sealed class EnrollTwoFactorCommandHandler
    : ICommandHandler<EnrollTwoFactorCommand, TwoFactorEnrollmentResponse>
{
    private readonly UserManager<FshUser> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly TwoFactorOptions _options;

    public EnrollTwoFactorCommandHandler(
        UserManager<FshUser> userManager,
        ICurrentUser currentUser,
        IOptions<TwoFactorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _userManager = userManager;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async ValueTask<TwoFactorEnrollmentResponse> Handle(
        EnrollTwoFactorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.GetUserId().ToString();
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.");

        // Always reset so calling enroll twice rotates the secret — prevents stale codes
        // from a prior incomplete enrollment from silently succeeding.
        await _userManager.ResetAuthenticatorKeyAsync(user);
        var sharedKey = await _userManager.GetAuthenticatorKeyAsync(user)
            ?? throw new CustomException("Failed to generate authenticator key.");

        var email = user.Email ?? user.UserName ?? user.Id;
        var authenticatorUri = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            UrlEncoder.Default.Encode(_options.Issuer),
            UrlEncoder.Default.Encode(email),
            sharedKey);

        return new TwoFactorEnrollmentResponse(sharedKey, authenticatorUri);
    }
}
