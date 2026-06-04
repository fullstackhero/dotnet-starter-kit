using System.Security.Claims;

namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Service for user registration and external authentication.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Registers a new user with password.
    /// </summary>
    Task<string> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string userName,
        string password,
        string confirmPassword,
        string phoneNumber,
        string origin,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets or creates a user from an external authentication principal.
    /// </summary>
    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a user's email address.
    /// </summary>
    Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken);

    /// <summary>
    /// Administratively marks a user's email as confirmed without a confirmation token. Gated by the
    /// <c>Permissions.Users.ConfirmEmail</c> permission at the endpoint. Idempotent.
    /// </summary>
    Task AdminConfirmEmailAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-sends the email-confirmation link to a user who has not yet confirmed their address.
    /// <paramref name="origin"/> is the request base URL used to build the confirmation link.
    /// Throws if the user's email is already confirmed.
    /// </summary>
    Task ResendConfirmationEmailAsync(string userId, string origin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a user's phone number.
    /// </summary>
    Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default);
}