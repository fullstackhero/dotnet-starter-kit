using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Configuration;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Features.v1.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Services;

internal sealed class PasswordHistoryService(
    IdentityDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IOptions<PasswordHistoryOptions> options) : IPasswordHistoryService
{
    private readonly PasswordHistoryOptions _options = options.Value;

    public async Task RecordPasswordChangeAsync(string userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (!_options.Enabled)
        {
            return;
        }

        var passwordHistory = new UserPasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            ChangedOnUtc = DateTime.UtcNow
        };

        db.UserPasswordHistories.Add(passwordHistory);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsPasswordUsedBeforeAsync(string userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (!_options.Enabled)
        {
            return false;
        }

        var recentPasswords = await db.UserPasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.ChangedOnUtc)
            .Take(_options.PasswordsToPreventReuse)
            .Select(h => h.PasswordHash)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return recentPasswords.Any(hash => hash == passwordHash);
    }

    public async Task CleanupOldPasswordHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (!_options.Enabled)
        {
            return;
        }

        var idsToDelete = await db.UserPasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.ChangedOnUtc)
            .Skip(_options.PasswordHistoryKeepCount)
            .Select(h => h.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (idsToDelete.Count > 0)
        {
            var entitiesToDelete = await db.UserPasswordHistories
                .Where(h => idsToDelete.Contains(h.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            db.UserPasswordHistories.RemoveRange(entitiesToDelete);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public int GetPasswordsToPreventReuse() => _options.PasswordsToPreventReuse;

    public int GetPasswordHistoryKeepCount() => _options.PasswordHistoryKeepCount;

    public bool IsEnabled() => _options.Enabled;
}
