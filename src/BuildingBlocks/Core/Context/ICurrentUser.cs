using System.Security.Claims;

namespace FSH.Framework.Core.Context;

/// <summary>
/// Represents the current authenticated user context with access to user information and claims.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the display name of the current user.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the unique identifier of the current user.
    /// </summary>
    /// <returns>The user's unique identifier.</returns>
    Guid GetUserId();

    /// <summary>
    /// Gets the email address of the current user.
    /// </summary>
    /// <returns>The user's email address, or null if not available.</returns>
    string? GetUserEmail();

    /// <summary>
    /// Gets the tenant identifier that the current user belongs to.
    /// </summary>
    /// <returns>The tenant identifier, or null if not in a multi-tenant context.</returns>
    string? GetTenant();

    /// <summary>
    /// Determines whether the current user is authenticated.
    /// </summary>
    /// <returns>true if the user is authenticated; otherwise, false.</returns>
    bool IsAuthenticated();

    /// <summary>
    /// Determines whether the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role to check for.</param>
    /// <returns>true if the user is in the specified role; otherwise, false.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Gets all claims associated with the current user.
    /// </summary>
    /// <returns>A collection of user claims, or null if no claims are available.</returns>
    IEnumerable<Claim>? GetUserClaims();
}