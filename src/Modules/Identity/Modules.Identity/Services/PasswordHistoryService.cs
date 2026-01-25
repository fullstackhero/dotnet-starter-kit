using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Services;

internal sealed class PasswordHistoryService : IPasswordHistoryService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<FshUser> _userManager;
    private readonly PasswordPolicyOptions _passwordPolicyOptions;

    public PasswordHistoryService(
        IdentityDbContext db,
        UserManager<FshUser> userManager,
        IOptions<PasswordPolicyOptions> passwordPolicyOptions)
    {
        _db = db;
        _userManager = userManager;
        _passwordPolicyOptions = passwordPolicyOptions.Value;
    }

    public async Task<bool> IsPasswordInHistoryAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(newPassword);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        // Get the last N passwords from history (where N = PasswordHistoryCount)
        var passwordHistoryCount = _passwordPolicyOptions.PasswordHistoryCount;
        if (passwordHistoryCount <= 0)
        {
            return false; // Password history check disabled
        }

        var recentPasswordHashes = await _db.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(passwordHistoryCount)
            .Select(ph => ph.PasswordHash)
            .ToListAsync(cancellationToken);

        // Check if the new password matches any recent password
        foreach (var passwordHash in recentPasswordHashes)
        {
            var passwordHasher = _userManager.PasswordHasher;
            var result = passwordHasher.VerifyHashedPassword(user, passwordHash, newPassword);

            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return true; // Password is in history
            }
        }

        return false; // Password is not in history
    }

    public async Task SavePasswordHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return;
        }

        var passwordHistoryEntry = PasswordHistory.Create(userId, user.PasswordHash);

        _db.Set<PasswordHistory>().Add(passwordHistoryEntry);
        await _db.SaveChangesAsync(cancellationToken);

        // Clean up old password history entries
        await CleanupOldPasswordHistoryAsync(userId, cancellationToken);
    }

    public async Task CleanupOldPasswordHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var passwordHistoryCount = _passwordPolicyOptions.PasswordHistoryCount;
        if (passwordHistoryCount <= 0)
        {
            return; // Password history disabled
        }

        // Get all password history entries for the user, ordered by most recent
        var allPasswordHistories = await _db.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync(cancellationToken);

        // Keep only the configured number of passwords
        if (allPasswordHistories.Count > passwordHistoryCount)
        {
            var oldPasswordHistories = allPasswordHistories
                .Skip(passwordHistoryCount)
                .ToList();

            _db.Set<PasswordHistory>().RemoveRange(oldPasswordHistories);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
