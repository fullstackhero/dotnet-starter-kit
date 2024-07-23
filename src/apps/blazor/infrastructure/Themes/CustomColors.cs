using MudBlazor;

namespace FSH.Starter.Blazor.Infrastructure.Themes;

public static class CustomColors
{
    public static readonly List<string> ThemeColors = new()
    {
        Light.Primary,
        Colors.Blue.Default,
        Colors.Purple.Default,
        Colors.Orange.Default,
        Colors.Red.Default,
        Colors.Amber.Default,
        Colors.DeepPurple.Default,
        Colors.Pink.Default,
        Colors.Indigo.Default,
        Colors.LightBlue.Default,
        Colors.Cyan.Default,
        Colors.Green.Default,
    };

    public static class Light
    {
        public const string Primary = "rgba(76,175,80,1)";
        public const string Secondary = "rgba(33,150,243,1)";
        public const string Background = "#FFF";
        public const string AppbarBackground = "#FFF";
        public const string AppbarText = "#6e6e6e";
    }

    public static class Dark
    {
        public const string Primary = "rgba(76,175,80,1)";
        public const string Secondary = "rgba(33,150,243,1)";
        public const string Background = "#1b1f22";
        public const string AppbarBackground = "#1b1f22";
        public const string DrawerBackground = "#121212";
        public const string Surface = "#202528";
        public const string Disabled = "#545454";
    }
}
