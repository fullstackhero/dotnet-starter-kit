using FSH.Modules.Identity.Contracts.DTOs;

namespace FSH.Modules.Identity.Contracts.Services;

public interface IPasswordExpiryService
{
    /// <summary>Check if a user's password has expired.</summary>
    Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Get the number of days until password expires (-1 if already expired).</summary>
    Task<int> GetDaysUntilExpiryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Check if password is expiring soon (within warning period).</summary>
    Task<bool> IsPasswordExpiringWithinWarningPeriodAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Get expiry status with detailed information.</summary>
    Task<PasswordExpiryStatusDto> GetPasswordExpiryStatusAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Update the last password change date for a user.</summary>
    Task UpdateLastPasswordChangeDateAsync(string userId, CancellationToken cancellationToken = default);
}