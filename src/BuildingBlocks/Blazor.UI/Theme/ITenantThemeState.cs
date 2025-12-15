namespace FSH.Framework.Blazor.UI.Theme;

/// <summary>
/// Service for managing tenant theme state in Blazor applications.
/// Handles theme loading, caching, and change notifications.
/// </summary>
public interface ITenantThemeState
{
    /// <summary>
    /// Gets the current theme settings.
    /// </summary>
    TenantThemeSettings Current { get; }

    /// <summary>
    /// Gets the current MudTheme built from settings.
    /// </summary>
    MudTheme Theme { get; }

    /// <summary>
    /// Gets or sets whether dark mode is enabled.
    /// </summary>
    bool IsDarkMode { get; set; }

    /// <summary>
    /// Event fired when theme settings change.
    /// </summary>
    event Action? OnThemeChanged;

    /// <summary>
    /// Loads theme settings from the API.
    /// </summary>
    Task LoadThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves current theme settings to the API.
    /// </summary>
    Task SaveThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets theme to defaults.
    /// </summary>
    Task ResetThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current theme settings without saving.
    /// </summary>
    void UpdateSettings(TenantThemeSettings settings);

    /// <summary>
    /// Toggles dark mode.
    /// </summary>
    void ToggleDarkMode();
}
