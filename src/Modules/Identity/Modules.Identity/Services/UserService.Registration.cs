using FSH.Framework.Core.Common;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Mailing;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Text;

namespace FSH.Modules.Identity.Services;

internal sealed partial class UserService
{
    public async Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
    {
        EnsureValidTenant();
        ArgumentNullException.ThrowIfNull(principal);

        var email = ExtractEmailFromPrincipal(principal);

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return existingUser.Id;
        }

        var user = await CreateUserFromPrincipalAsync(principal, email);
        await AssignDefaultRoleAndGroupsAsync(user, "ExternalAuth");
        await PublishUserRegisteredAsync(user, "Identity.ExternalAuth");

        return user.Id;
    }

    private static string ExtractEmailFromPrincipal(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? throw new CustomException("Email claim is required for external authentication.");
    }

    private async Task<FshUser> CreateUserFromPrincipalAsync(ClaimsPrincipal principal, string email)
    {
        var (firstName, lastName, userName) = ExtractUserInfoFromPrincipal(principal, email);

        userName = await EnsureUniqueUserNameAsync(userName);

        var user = new FshUser
        {
            Email = email,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException("Failed to create user from external principal.", errors);
        }

        return user;
    }

    private static (string firstName, string lastName, string userName) ExtractUserInfoFromPrincipal(
        ClaimsPrincipal principal, string email)
    {
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName)
            ?? principal.FindFirstValue("given_name")
            ?? string.Empty;

        var lastName = principal.FindFirstValue(ClaimTypes.Surname)
            ?? principal.FindFirstValue("family_name")
            ?? string.Empty;

        var userName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("preferred_username")
            ?? email.Split('@')[0];

        return (firstName, lastName, userName);
    }

    private async Task<string> EnsureUniqueUserNameAsync(string userName)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return $"{userName}_{Guid.NewGuid():N}"[..20];
        }
        return userName;
    }

    public async Task<string> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string userName,
        string password,
        string confirmPassword,
        string phoneNumber,
        string origin,
        CancellationToken cancellationToken)
    {
        ValidatePasswordMatch(password, confirmPassword);

        var user = await CreateUserWithPasswordAsync(firstName, lastName, email, userName, password, phoneNumber);
        await AssignDefaultRoleAndGroupsAsync(user, "System", cancellationToken);
        await SendConfirmationEmailAsync(user, origin, cancellationToken);
        await PublishUserRegisteredAsync(user, "Identity", cancellationToken);

        return user.Id;
    }

    private static void ValidatePasswordMatch(string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            throw new CustomException("password mismatch.");
        }
    }

    private async Task<FshUser> CreateUserWithPasswordAsync(
        string firstName,
        string lastName,
        string email,
        string userName,
        string password,
        string phoneNumber)
    {
        var user = new FshUser
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserName = userName,
            PhoneNumber = phoneNumber,
            IsActive = true,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new CustomException("error while registering a new user", errors);
        }

        return user;
    }

    private async Task AssignDefaultRoleAndGroupsAsync(
        FshUser user,
        string source,
        CancellationToken cancellationToken = default)
    {
        await userManager.AddToRoleAsync(user, RoleConstants.Basic);

        var defaultGroups = await db.Groups
            .Where(g => g.IsDefault && !g.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var group in defaultGroups)
        {
            db.UserGroups.Add(UserGroup.Create(user.Id, group.Id, source));
        }

        if (defaultGroups.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SendConfirmationEmailAsync(FshUser user, string origin, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            return;
        }

        string emailVerificationUri = await GetEmailVerificationUriAsync(user, origin);
        string emailBody = BuildConfirmationEmailHtml(user.FirstName ?? user.UserName ?? "User", emailVerificationUri);

        var mailRequest = new MailRequest(
            new Collection<string> { user.Email },
            "Confirm Your Email Address",
            emailBody);

        jobService.Enqueue("email", () => mailService.SendAsync(mailRequest, cancellationToken));
    }

    private async Task PublishUserRegisteredAsync(
        FshUser user,
        string source,
        CancellationToken cancellationToken = default)
    {
        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        user.RecordRegistered(tenantId);

        await db.SaveChangesAsync(cancellationToken);

        var integrationEvent = new UserRegisteredIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenantId,
            CorrelationId: Guid.NewGuid().ToString(),
            Source: source,
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            FirstName: user.FirstName ?? string.Empty,
            LastName: user.LastName ?? string.Empty);

        await outboxStore.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetEmailVerificationUriAsync(FshUser user, string origin)
    {
        EnsureValidTenant();

        string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        const string route = "api/v1/identity/confirm-email";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));

        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), QueryStringKeys.UserId, user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, QueryStringKeys.Code, code);
        verificationUri = QueryHelpers.AddQueryString(
            verificationUri,
            MultitenancyConstants.Identifier,
            multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id!);

        return verificationUri;
    }

    private static string BuildConfirmationEmailHtml(string userName, string confirmationUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Confirm Your Email</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;">
                <table role="presentation" style="width: 100%; border-collapse: collapse;">
                    <tr>
                        <td align="center" style="padding: 40px 0;">
                            <table role="presentation" style="width: 100%; max-width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                                <tr>
                                    <td style="padding: 40px 40px 30px 40px; text-align: center; background-color: #2563eb; border-radius: 8px 8px 0 0;">
                                        <h1 style="margin: 0; color: #ffffff; font-size: 24px; font-weight: 600;">Confirm Your Email Address</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 40px;">
                                        <p style="margin: 0 0 20px 0; color: #334155; font-size: 16px; line-height: 1.6;">
                                            Hi {System.Net.WebUtility.HtmlEncode(userName)},
                                        </p>
                                        <p style="margin: 0 0 20px 0; color: #334155; font-size: 16px; line-height: 1.6;">
                                            Thank you for registering! Please confirm your email address by clicking the button below:
                                        </p>
                                        <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                            <tr>
                                                <td align="center" style="padding: 30px 0;">
                                                    <a href="{System.Net.WebUtility.HtmlEncode(confirmationUrl)}" style="display: inline-block; padding: 14px 32px; background-color: #2563eb; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 6px;">
                                                        Confirm Email Address
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>
                                        <p style="margin: 0 0 20px 0; color: #64748b; font-size: 14px; line-height: 1.6;">
                                            If the button doesn't work, copy and paste this link into your browser:
                                        </p>
                                        <p style="margin: 0 0 20px 0; color: #2563eb; font-size: 14px; line-height: 1.6; word-break: break-all;">
                                            {System.Net.WebUtility.HtmlEncode(confirmationUrl)}
                                        </p>
                                        <p style="margin: 30px 0 0 0; color: #64748b; font-size: 14px; line-height: 1.6;">
                                            If you didn't create an account, you can safely ignore this email.
                                        </p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 20px 40px; background-color: #f1f5f9; border-radius: 0 0 8px 8px; text-align: center;">
                                        <p style="margin: 0; color: #94a3b8; font-size: 12px;">
                                            This is an automated message. Please do not reply to this email.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
