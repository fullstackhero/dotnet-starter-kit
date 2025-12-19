namespace FSH.Modules.Identity.Configuration;

/// <summary>
/// Configuration options for password history management.
/// </summary>
public class PasswordHistoryOptions
{
    /// <summary>
    /// Gets or sets the number of previous passwords to prevent reuse.
    /// Default is 5.
    /// </summary>
    public int PasswordsToPreventReuse { get; set; } = 5;

    /// <summary>
    /// Gets or sets the number of previous password entries to keep in history.
    /// Older entries will be automatically cleaned up.
    /// Default is 10.
    /// </summary>
    public int PasswordHistoryKeepCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether password history is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
