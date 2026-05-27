using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Services;

internal sealed class PasswordExpiryService : IPasswordExpiryService
{
    private readonly UserManager<FshUser> _userManager;
    private readonly PasswordPolicyOptions _passwordPolicyOptions;
    private readonly TimeProvider _timeProvider;

    public PasswordExpiryService(
        UserManager<FshUser> userManager,
        IOptions<PasswordPolicyOptions> passwordPolicyOptions,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _passwordPolicyOptions = passwordPolicyOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        return IsPasswordExpired(user);
    }

    public async Task<int> GetDaysUntilExpiryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return int.MaxValue;
        }

        return GetDaysUntilExpiry(user);
    }

    public async Task<bool> IsPasswordExpiringWithinWarningPeriodAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        return IsPasswordExpiringWithinWarningPeriod(user);
    }

    public async Task<PasswordExpiryStatusDto> GetPasswordExpiryStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new PasswordExpiryStatusDto
            {
                IsExpired = false,
                IsExpiringWithinWarningPeriod = false,
                DaysUntilExpiry = int.MaxValue,
                ExpiryDate = null
            };
        }

        return GetPasswordExpiryStatus(user);
    }

    public async Task UpdateLastPasswordChangeDateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            user.LastPasswordChangeDate = _timeProvider.GetUtcNow().UtcDateTime;
            await _userManager.UpdateAsync(user);
        }
    }

    // Internal helpers that work with FshUser directly
    private bool IsPasswordExpired(FshUser user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return false;
        }

        var expiryDate = user.LastPasswordChangeDate.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        return _timeProvider.GetUtcNow().UtcDateTime > expiryDate;
    }

    private int GetDaysUntilExpiry(FshUser user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return int.MaxValue;
        }

        var expiryDate = user.LastPasswordChangeDate.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        var daysUntilExpiry = (int)(expiryDate - _timeProvider.GetUtcNow().UtcDateTime).TotalDays;
        return daysUntilExpiry;
    }

    private bool IsPasswordExpiringWithinWarningPeriod(FshUser user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return false;
        }

        var daysUntilExpiry = GetDaysUntilExpiry(user);
        return daysUntilExpiry >= 0 && daysUntilExpiry <= _passwordPolicyOptions.PasswordExpiryWarningDays;
    }

    private PasswordExpiryStatusDto GetPasswordExpiryStatus(FshUser user)
    {
        var expiryDate = user.LastPasswordChangeDate.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        var daysUntilExpiry = GetDaysUntilExpiry(user);
        var isExpired = IsPasswordExpired(user);
        var isExpiringWithinWarningPeriod = IsPasswordExpiringWithinWarningPeriod(user);

        return new PasswordExpiryStatusDto
        {
            IsExpired = isExpired,
            IsExpiringWithinWarningPeriod = isExpiringWithinWarningPeriod,
            DaysUntilExpiry = daysUntilExpiry,
            ExpiryDate = _passwordPolicyOptions.EnforcePasswordExpiry ? expiryDate : null
        };
    }
}