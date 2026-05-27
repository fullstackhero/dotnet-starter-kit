namespace FSH.Modules.Identity.Contracts.Services;

public interface IPasswordHistoryService
{
    /// <summary>Check if the new password matches any recent passwords in history.</summary>
    Task<bool> IsPasswordInHistoryAsync(string userId, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Save the current password hash to history after a password change.</summary>
    Task SavePasswordHistoryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Remove old password history entries beyond the configured retention count.</summary>
    Task CleanupOldPasswordHistoryAsync(string userId, CancellationToken cancellationToken = default);
}