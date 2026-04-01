namespace FSH.Framework.Shared.Localization;

/// <summary>
/// Defines supported cultures and localization defaults for the application.
/// </summary>
public static class LocalizationConstants
{
    public const string DefaultCulture = "en-US";
    public const string CultureCookieName = ".FSH.Culture";

    /// <summary>Cultures supported across all projects.</summary>
    public static readonly string[] SupportedCultures =
    [
        "en-US"
    ];
}
