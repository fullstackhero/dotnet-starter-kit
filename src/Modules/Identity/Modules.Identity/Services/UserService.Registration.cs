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

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? throw new CustomException("Email claim is required for external authentication.");

        // Try to find existing user by email
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user.Id;
        }

        // Extract claims for new user creation
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName)
            ?? principal.FindFirstValue("given_name")
            ?? string.Empty;

        var lastName = principal.FindFirstValue(ClaimTypes.Surname)
            ?? principal.FindFirstValue("family_name")
            ?? string.Empty;

        var userName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("preferred_username")
            ?? email.Split('@')[0];

        // Ensure unique username
        if (await userManager.FindByNameAsync(userName) is not null)
        {
            userName = $"{userName}_{Guid.NewGuid():N}"[..20];
        }

        // Create new user from external principal
        user = new FshUser
        {
            Email = email,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true, // External provider has verified the email
            PhoneNumberConfirmed = false,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException("Failed to create user from external principal.", errors);
        }

        // Assign basic role
        await userManager.AddToRoleAsync(user, RoleConstants.Basic);

        // Add to default groups
        var defaultGroups = await db.Groups
            .Where(g => g.IsDefault && !g.IsDeleted)
            .ToListAsync();

        foreach (var group in defaultGroups)
        {
            db.UserGroups.Add(UserGroup.Create(user.Id, group.Id, "ExternalAuth"));
        }

        // Raise domain event for user registration
        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        user.RecordRegistered(tenantId);

        // Save to dispatch domain event via interceptor
        await db.SaveChangesAsync();

        // Publish integration event
        var integrationEvent = new UserRegisteredIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenantId,
            CorrelationId: Guid.NewGuid().ToString(),
            Source: "Identity.ExternalAuth",
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            FirstName: user.FirstName ?? string.Empty,
            LastName: user.LastName ?? string.Empty);

        await outboxStore.AddAsync(integrationEvent).ConfigureAwait(false);

        return user.Id;
    }

    public async Task<string> RegisterAsync(string firstName, string lastName, string email, string userName, string password, string confirmPassword, string phoneNumber, string origin, CancellationToken cancellationToken)
    {
        if (password != confirmPassword) throw new CustomException("password mismatch.");

        // create user entity
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

        // register user
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new CustomException("error while registering a new user", errors);
        }

        // add basic role
        await userManager.AddToRoleAsync(user, RoleConstants.Basic);

        // add user to default groups
        var defaultGroups = await db.Groups
            .Where(g => g.IsDefault && !g.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var group in defaultGroups)
        {
            db.UserGroups.Add(UserGroup.Create(user.Id, group.Id, "System"));
        }

        if (defaultGroups.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        // send confirmation mail
        if (!string.IsNullOrEmpty(user.Email))
        {
            string emailVerificationUri = await GetEmailVerificationUriAsync(user, origin);
            string emailBody = BuildConfirmationEmailHtml(user.FirstName ?? user.UserName ?? "User", emailVerificationUri);
            var mailRequest = new MailRequest(
                new Collection<string> { user.Email },
                "Confirm Your Email Address",
                emailBody);
            jobService.Enqueue("email", () => mailService.SendAsync(mailRequest, cancellationToken));
        }

        // Raise domain event for user registration
        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        user.RecordRegistered(tenantId);

        // Save to dispatch domain event via interceptor
        await db.SaveChangesAsync(cancellationToken);

        // enqueue integration event for user registration
        var correlationId = Guid.NewGuid().ToString();
        var integrationEvent = new UserRegisteredIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenantId,
            CorrelationId: correlationId,
            Source: "Identity",
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            FirstName: user.FirstName ?? string.Empty,
            LastName: user.LastName ?? string.Empty);

        await outboxStore.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        return user.Id;
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
        verificationUri = QueryHelpers.AddQueryString(verificationUri,
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
