using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.ObjectModel;
using System.Text;

namespace FSH.Modules.Identity.Services;

internal sealed class UserPasswordService(
    UserManager<FshUser> userManager,
    IdentityDbContext db,
    IJobService jobService,
    IMailService mailService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IPasswordHistoryService passwordHistoryService,
    IPasswordExpiryService passwordExpiryService) : IUserPasswordService
{
    public async Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var user = await userManager.FindByEmailAsync(email);

        // Anti-enumeration: respond identically regardless of registration — a real user gets the
        // reset email; an unknown or email-less account silently no-ops with the same 200.
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // Build the SPA reset link with QueryHelpers (matches GetEmailVerificationUriAsync): trim any trailing
        // slash from the configured origin (Uri.ToString adds one for a host-only URL → "//reset-password"
        // misses the client route) and include the tenant the reset page requires. QueryHelpers URL-encodes
        // each value, so reserved chars in the email (e.g. '+') survive.
        var resetPasswordUri = QueryHelpers.AddQueryString(
            $"{origin.TrimEnd('/')}/reset-password",
            new Dictionary<string, string?>
            {
                ["token"] = token,
                ["email"] = email,
                ["tenant"] = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id,
            });
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

        // Raise domain event for password reset
        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        user.RecordPasswordChanged(wasReset: true, tenantId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("user not found");

        var result = await userManager.ChangePasswordAsync(user, password, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException("failed to change password", errors);
        }

        // Raise domain event for password change
        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        user.RecordPasswordChanged(wasReset: false, tenantId);
        await db.SaveChangesAsync(cancellationToken);

        // Update password expiry date
        await passwordExpiryService.UpdateLastPasswordChangeDateAsync(userId, cancellationToken);

        // Save to history
        await passwordHistoryService.SavePasswordHistoryAsync(userId, cancellationToken);
    }

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
    }
}