using FSH.Modules.Identity.Configuration;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Features.v1.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Services;

internal sealed class PasswordExpiryService(
    UserManager<FshUser> userManager,
    IdentityDbContext db,
    IOptions<PasswordExpiryOptions> options) : IPasswordExpiryService
{
    private readonly PasswordExpiryOptions _options = options.Value;

    public async Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (!_options.Enabled || _options.PasswordExpiryDays <= 0)
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user?.LastPasswordChangeUtc is null)
        {
            return false;
        }

        var expiryDate = user.LastPasswordChangeUtc.Value.AddDays(_options.PasswordExpiryDays);
        return DateTime.UtcNow > expiryDate;
    }

    public async Task<int> GetDaysUntilPasswordExpiryAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (!_options.Enabled || _options.PasswordExpiryDays <= 0)
        {
            return int.MaxValue;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user?.LastPasswordChangeUtc is null)
        {
            return int.MaxValue;
        }

        var expiryDate = user.LastPasswordChangeUtc.Value.AddDays(_options.PasswordExpiryDays);
        var daysRemaining = (int)(expiryDate - DateTime.UtcNow).TotalDays;
        return daysRemaining;
    }

    public async Task<bool> ShouldWarnAboutPasswordExpiryAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (!_options.Enabled || _options.PasswordExpiryDays <= 0)
        {
            return false;
        }

        var daysUntilExpiry = await GetDaysUntilPasswordExpiryAsync(userId, cancellationToken);
        return daysUntilExpiry <= _options.WarningDaysBeforeExpiry && daysUntilExpiry > 0;
    }

    public async Task UpdateLastPasswordChangeAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (!_options.Enabled)
        {
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return;
        }

        user.LastPasswordChangeUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user).ConfigureAwait(false);
    }

    public bool IsEnabled() => _options.Enabled;

    public int GetPasswordExpiryDays() => _options.PasswordExpiryDays;

    public int GetWarningDaysBeforeExpiry() => _options.WarningDaysBeforeExpiry;
}
