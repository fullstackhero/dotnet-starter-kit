namespace FSH.Modules.Identity.Contracts.Services;

public interface IPasswordHistoryService
{
    /// <summary>Records a password change in the password history.</summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="passwordHash">The hashed password to record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordPasswordChangeAsync(string userId, string passwordHash, CancellationToken cancellationToken = default);

    /// <summary>Checks if the given password has been used previously by the user.</summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="passwordHash">The hashed password to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the password has been used before; otherwise, false.</returns>
    Task<bool> IsPasswordUsedBeforeAsync(string userId, string passwordHash, CancellationToken cancellationToken = default);

    /// <summary>Removes old password history entries for a user, keeping only the configured count.</summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CleanupOldPasswordHistoryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Gets the configured number of previous passwords to prevent reuse.</summary>
    /// <returns>The number of passwords to prevent reuse.</returns>
    int GetPasswordsToPreventReuse();

    /// <summary>Gets the configured number of password history entries to keep.</summary>
    /// <returns>The number of entries to keep.</returns>
    int GetPasswordHistoryKeepCount();

    /// <summary>Gets whether password history is enabled.</summary>
    /// <returns>True if enabled; otherwise, false.</returns>
    bool IsEnabled();
}
