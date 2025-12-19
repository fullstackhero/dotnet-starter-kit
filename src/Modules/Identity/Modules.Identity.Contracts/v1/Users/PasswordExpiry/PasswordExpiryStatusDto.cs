namespace FSH.Modules.Identity.Contracts.v1.Users.PasswordExpiry;

public class PasswordExpiryStatusDto
{
    /// <summary>
    /// Whether the user's password has expired.
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// The number of days until the password expires. Negative if already expired.
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// Whether the user should be warned about upcoming expiry.
    /// </summary>
    public bool ShouldWarn { get; set; }

    /// <summary>
    /// The configured password expiry days.
    /// </summary>
    public int PasswordExpiryDays { get; set; }

    /// <summary>
    /// The configured warning days before expiry.
    /// </summary>
    public int WarningDaysBeforeExpiry { get; set; }
}
