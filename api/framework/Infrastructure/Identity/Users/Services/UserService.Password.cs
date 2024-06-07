using System.Collections.ObjectModel;
using System.Text;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Users.Features.ForgotPassword;
using FSH.Framework.Core.Identity.Users.Features.ResetPassword;
using FSH.Framework.Core.Mail;
using Microsoft.AspNetCore.WebUtilities;

namespace FSH.Framework.Infrastructure.Identity.Users.Services;
internal sealed partial class UserService
{
    public async Task ForgotPasswordAsync(ForgotPasswordCommand request, string origin, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("User email cannot be null or empty.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetPasswordUri = $"{origin}/reset-password?token={token}&email={request.Email}";
        var mailRequest = new MailRequest(
            new Collection<string> { user.Email },
            "Reset Password",
            $"Please reset your password using the following link: {resetPasswordUri}");

        jobService.Enqueue(() => mailService.SendAsync(mailRequest, CancellationToken.None));
    }

    public async Task ResetPasswordAsync(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        request.Token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new FshException("Error resetting password", errors);
        }
    }
}
