using System.Security.Claims;

namespace FSH.Framework.Core.Context;

/// <summary>
/// Provides methods to initialize and set the current user context.
/// </summary>
public interface ICurrentUserInitializer
{
    /// <summary>
    /// Sets the current user from a claims principal.
    /// </summary>
    /// <param name="user">The claims principal representing the authenticated user.</param>
    void SetCurrentUser(ClaimsPrincipal user);

    /// <summary>
    /// Sets the current user identifier directly.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    void SetCurrentUserId(string userId);
}