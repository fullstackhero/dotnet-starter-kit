namespace FSH.Modules.Identity.Data;

public class PasswordPolicyOptions
{
    /// <summary>Number of previous passwords to keep in history (prevent reuse)</summary>
    public int PasswordHistoryCount { get; set; } = 5;

    /// <summary>Number of days before password expires and must be changed</summary>
    public int PasswordExpiryDays { get; set; } = 90;

    /// <summary>Number of days before expiry to show warning to user</summary>
    public int PasswordExpiryWarningDays { get; set; } = 14;

    /// <summary>Set to false to disable password expiry enforcement</summary>
    public bool EnforcePasswordExpiry { get; set; } = true;
}