using MudBlazor;

namespace FSH.Starter.Blazor.Infrastructure.Preferences;

public interface IClientPreferenceManager : IPreferenceManager
{
    Task<MudTheme> GetCurrentThemeAsync();

    Task<bool> ToggleDarkModeAsync();

    Task<bool> ToggleDrawerAsync();

    Task<bool> ChangeLanguageAsync(string languageCode);

    Task<bool> ToggleLayoutDirectionAsync();
}