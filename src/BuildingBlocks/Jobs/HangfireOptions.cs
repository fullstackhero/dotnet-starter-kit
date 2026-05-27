using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Jobs;

public sealed class HangfireOptions
{
    /// <summary>
    /// Username required to access the Hangfire dashboard. MUST be set via configuration
    /// in any non-dev environment — there is no safe default.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string UserName { get; set; } = default!;

    /// <summary>
    /// Password required to access the Hangfire dashboard. MUST be set via configuration
    /// (user secrets locally, env vars or Key Vault in prod). Short or empty passwords are
    /// rejected at startup by <c>ValidateDataAnnotations().ValidateOnStart()</c>.
    /// </summary>
    [Required]
    [MinLength(12)]
    public string Password { get; set; } = default!;

    public string Route { get; set; } = "/jobs";
}
