using FSH.Framework.Core.Exceptions;
using FSH.Framework.Mailing;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Features.v1.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.ObjectModel;
using System.Text;

namespace FSH.Framework.Infrastructure.Identity.Users.Services;

internal sealed partial class UserService
{
    public async Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new NotFoundException("user not found");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("user email cannot be null or empty");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetPasswordUri = $"{origin}/reset-password?token={token}&email={email}";
        var mailRequest = new MailRequest(
            new Collection<string> { user.Email },
            "Reset Password",
            $"Please reset your password using the following link: {resetPasswordUri}");

        jobService.Enqueue(() => mailService.SendAsync(mailRequest, CancellationToken.None));
    }

    public async Task ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new NotFoundException("user not found");
        }

        token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await userManager.ResetPasswordAsync(user, token, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException("error resetting password", errors);
        }

        // Record the password reset in history
        var updatedUser = await userManager.FindByIdAsync(user.Id).ConfigureAwait(false);
        if (updatedUser?.PasswordHash is not null)
        {
            await passwordHistoryService.RecordPasswordChangeAsync(user.Id, updatedUser.PasswordHash, cancellationToken).ConfigureAwait(false);
            await passwordHistoryService.CleanupOldPasswordHistoryAsync(user.Id, cancellationToken).ConfigureAwait(false);
        }

        // Update password change timestamp for expiry tracking
        await passwordExpiryService.UpdateLastPasswordChangeAsync(user.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("user not found");

        var result = await userManager.ChangePasswordAsync(user, password, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException("failed to change password", errors);
        }
    }
}