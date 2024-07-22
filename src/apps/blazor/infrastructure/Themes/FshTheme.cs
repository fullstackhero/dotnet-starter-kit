using MudBlazor;

namespace FSH.Starter.Blazor.Infrastructure.Themes;

public class FshTheme : MudTheme
{
    public FshTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = CustomColors.Light.Primary,
            Secondary = CustomColors.Light.Secondary,
            Background = CustomColors.Light.Background,
            AppbarBackground = CustomColors.Light.AppbarBackground,
            AppbarText = CustomColors.Light.AppbarText,
            DrawerBackground = CustomColors.Light.Background,
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = CustomColors.Light.Primary,
            TableLines = "#e0e0e029",
            OverlayDark = "hsl(0deg 0% 0% / 75%)"
        };

        PaletteDark = new PaletteDark()
        {
            Primary = CustomColors.Dark.Primary,
            Secondary = CustomColors.Dark.Secondary,
            Success = CustomColors.Dark.Primary,
            Black = "#27272f",
            Background = CustomColors.Dark.Background,
            Surface = CustomColors.Dark.Surface,
            DrawerBackground = CustomColors.Dark.DrawerBackground,
            DrawerText = "rgba(255,255,255, 0.50)",
            AppbarBackground = CustomColors.Dark.AppbarBackground,
            AppbarText = "rgba(255,255,255, 0.70)",
            TextPrimary = "rgba(255,255,255, 0.70)",
            TextSecondary = "rgba(255,255,255, 0.50)",
            ActionDefault = "#adadb1",
            ActionDisabled = "rgba(255,255,255, 0.26)",
            ActionDisabledBackground = "rgba(255,255,255, 0.12)",
            DrawerIcon = "rgba(255,255,255, 0.50)",
            TableLines = "#e0e0e036",
            Dark = CustomColors.Dark.DrawerBackground,
            Divider = "#e0e0e036",
            OverlayDark = "hsl(0deg 0% 0% / 75%)",
            TextDisabled = CustomColors.Dark.Disabled
        };

        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "5px"
        };

        Typography = CustomTypography.FshTypography;
        Shadows = new Shadow();
        ZIndex = new ZIndex() { Drawer = 1300 };
    }
}