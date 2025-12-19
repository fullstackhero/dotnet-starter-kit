namespace FSH.Modules.Identity.Configuration;

/// <summary>
/// Configuration options for password expiry management.
/// </summary>
public class PasswordExpiryOptions
{
    /// <summary>
    /// Gets or sets the number of days before a password expires.
    /// Default is 90 days. Set to 0 or negative to disable expiry.
    /// </summary>
    public int PasswordExpiryDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets whether password expiry is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days before expiry to warn users about password expiry.
    /// Default is 14 days.
    /// </summary>
    public int WarningDaysBeforeExpiry { get; set; } = 14;
}
