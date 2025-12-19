namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Service for managing password expiry.
/// </summary>
public interface IPasswordExpiryService
{
    /// <summary>
    /// Checks if a user's password has expired.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the password has expired; otherwise, false.</returns>
    Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of days until the user's password expires.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of days until expiry. Negative value indicates already expired.</returns>
    Task<int> GetDaysUntilPasswordExpiryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user should be warned about upcoming password expiry.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the user is within the warning period; otherwise, false.</returns>
    Task<bool> ShouldWarnAboutPasswordExpiryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last password change date for a user to the current UTC time.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateLastPasswordChangeAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether password expiry is enabled.
    /// </summary>
    /// <returns>True if enabled; otherwise, false.</returns>
    bool IsEnabled();

    /// <summary>
    /// Gets the configured password expiry days.
    /// </summary>
    /// <returns>The number of days before password expires.</returns>
    int GetPasswordExpiryDays();

    /// <summary>
    /// Gets the configured warning days before expiry.
    /// </summary>
    /// <returns>The number of days before expiry to warn the user.</returns>
    int GetWarningDaysBeforeExpiry();
}
