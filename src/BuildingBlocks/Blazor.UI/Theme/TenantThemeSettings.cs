using MudBlazor;

namespace FSH.Framework.Blazor.UI.Theme;

/// <summary>
/// Client-side representation of tenant theme settings.
/// Used by Blazor components to build MudTheme dynamically.
/// </summary>
public sealed class TenantThemeSettings
{
    public PaletteSettings LightPalette { get; set; } = new();
    public PaletteSettings DarkPalette { get; set; } = PaletteSettings.DefaultDark;
    public BrandAssets BrandAssets { get; set; } = new();
    public TypographySettings Typography { get; set; } = new();
    public LayoutSettings Layout { get; set; } = new();
    public bool IsDefault { get; set; } = true;

    public static TenantThemeSettings Default => new();

    public MudTheme ToMudTheme()
    {
        var bodyFontFamily = new[] { Typography.FontFamily.Split(',')[0].Trim(), "system-ui", "sans-serif" };
        var headingFontFamily = new[] { Typography.HeadingFontFamily.Split(',')[0].Trim(), "system-ui", "sans-serif" };

        return new MudTheme
        {
            PaletteLight = LightPalette.ToPaletteLight(),
            PaletteDark = DarkPalette.ToPaletteDark(),
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = Layout.BorderRadius
            },
            Typography = new MudBlazor.Typography
            {
                Default =
                {
                    FontFamily = bodyFontFamily,
                    FontSize = $"{Typography.FontSizeBase / 16.0:F4}rem",
                    LineHeight = Typography.LineHeightBase.ToString("F2")
                },
                H1 = { FontFamily = headingFontFamily },
                H2 = { FontFamily = headingFontFamily },
                H3 = { FontFamily = headingFontFamily },
                H4 = { FontFamily = headingFontFamily },
                H5 = { FontFamily = headingFontFamily },
                H6 = { FontFamily = headingFontFamily }
            }
        };
    }
}

public sealed class PaletteSettings
{
    public string Primary { get; set; } = "#2563EB";
    public string Secondary { get; set; } = "#0F172A";
    public string Tertiary { get; set; } = "#6366F1";
    public string Background { get; set; } = "#F8FAFC";
    public string Surface { get; set; } = "#FFFFFF";
    public string Error { get; set; } = "#DC2626";
    public string Warning { get; set; } = "#F59E0B";
    public string Success { get; set; } = "#16A34A";
    public string Info { get; set; } = "#0284C7";

    public static PaletteSettings DefaultLight => new();

    public static PaletteSettings DefaultDark => new()
    {
        Primary = "#38BDF8",
        Secondary = "#94A3B8",
        Tertiary = "#818CF8",
        Background = "#0B1220",
        Surface = "#111827",
        Error = "#F87171",
        Warning = "#FBBF24",
        Success = "#22C55E",
        Info = "#38BDF8"
    };

    public PaletteLight ToPaletteLight()
    {
        return new PaletteLight
        {
            Primary = Primary,
            Secondary = Secondary,
            Tertiary = Tertiary,
            Background = Background,
            Surface = Surface,
            AppbarBackground = Background,
            AppbarText = Secondary,
            DrawerBackground = Surface,
            TextPrimary = Secondary,
            TextSecondary = "#475569",
            Info = Info,
            Success = Success,
            Warning = Warning,
            Error = Error,
            TableLines = "#E2E8F0",
            Divider = "#E2E8F0"
        };
    }

    public PaletteDark ToPaletteDark()
    {
        return new PaletteDark
        {
            Primary = Primary,
            Secondary = Secondary,
            Tertiary = Tertiary,
            Background = Background,
            Surface = Surface,
            AppbarBackground = Background,
            AppbarText = "#E2E8F0",
            DrawerBackground = Background,
            TextPrimary = "#E2E8F0",
            TextSecondary = "#CBD5E1",
            Info = Info,
            Success = Success,
            Warning = Warning,
            Error = Error,
            TableLines = "#1F2937",
            Divider = "#1F2937"
        };
    }

    public PaletteSettings Clone() => new()
    {
        Primary = Primary,
        Secondary = Secondary,
        Tertiary = Tertiary,
        Background = Background,
        Surface = Surface,
        Error = Error,
        Warning = Warning,
        Success = Success,
        Info = Info
    };
}

public sealed class BrandAssets
{
    // Current URLs (returned from API)
    public string? LogoUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? FaviconUrl { get; set; }

    // Pending file uploads (same pattern as profile picture: FileName, ContentType, Data as byte[])
    public FileUpload? Logo { get; set; }
    public FileUpload? LogoDark { get; set; }
    public FileUpload? Favicon { get; set; }

    // Delete flags
    public bool DeleteLogo { get; set; }
    public bool DeleteLogoDark { get; set; }
    public bool DeleteFavicon { get; set; }
}

/// <summary>
/// File upload data matching the API's FileUploadRequest pattern.
/// </summary>
public sealed class FileUpload
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public byte[] Data { get; set; } = [];
}

public sealed class TypographySettings
{
    public string FontFamily { get; set; } = "Inter, sans-serif";
    public string HeadingFontFamily { get; set; } = "Inter, sans-serif";
    public double FontSizeBase { get; set; } = 14;
    public double LineHeightBase { get; set; } = 1.5;

    public static IReadOnlyList<string> WebSafeFonts =>
    [
        "Inter, sans-serif",
        "Arial, sans-serif",
        "Helvetica, sans-serif",
        "Georgia, serif",
        "Times New Roman, serif",
        "Verdana, sans-serif",
        "Tahoma, sans-serif",
        "Trebuchet MS, sans-serif",
        "Courier New, monospace",
        "Lucida Console, monospace",
        "Segoe UI, sans-serif",
        "Roboto, sans-serif",
        "Open Sans, sans-serif",
        "system-ui, sans-serif"
    ];

    public TypographySettings Clone() => new()
    {
        FontFamily = FontFamily,
        HeadingFontFamily = HeadingFontFamily,
        FontSizeBase = FontSizeBase,
        LineHeightBase = LineHeightBase
    };
}

public sealed class LayoutSettings
{
    public string BorderRadius { get; set; } = "4px";
    public int DefaultElevation { get; set; } = 1;

    public static IReadOnlyList<string> BorderRadiusOptions =>
    [
        "0px",
        "2px",
        "4px",
        "6px",
        "8px",
        "12px",
        "16px",
        "0.25rem",
        "0.5rem",
        "1rem"
    ];

    public LayoutSettings Clone() => new()
    {
        BorderRadius = BorderRadius,
        DefaultElevation = DefaultElevation
    };
}
