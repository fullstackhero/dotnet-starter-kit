namespace FSH.Framework.Core.Abstractions;

/// <summary>
/// Represents a basic application user with common properties.
/// </summary>
public interface IAppUser
{
    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    string? FirstName { get; }

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    string? LastName { get; }

    /// <summary>
    /// Gets the URL of the user's profile image.
    /// </summary>
    Uri? ImageUrl { get; }

    /// <summary>
    /// Gets a value indicating whether the user account is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the refresh token for the user's authentication session.
    /// </summary>
    string? RefreshToken { get; }

    /// <summary>
    /// Gets the expiry time of the refresh token.
    /// </summary>
    DateTime RefreshTokenExpiryTime { get; }
}