using System.ComponentModel;
using FSH.Framework.Shared.Storage;

namespace FSH.Modules.Multitenancy.Contracts.Dtos;

/// <remarks>
/// Marked <see cref="ImmutableObjectAttribute"/> + <c>sealed</c> so HybridCache can reuse the
/// in-process instance across requests without re-deserializing the JSON payload on every L1 hit.
/// DO NOT add mutable properties or make this class open — it would break HybridCache's L1
/// reuse optimization.
/// </remarks>
[ImmutableObject(true)]
public sealed record TenantThemeDto
{
    public PaletteDto LightPalette { get; init; } = new();
    public PaletteDto DarkPalette { get; init; } = new();
    public BrandAssetsDto BrandAssets { get; init; } = new();
    public TypographyDto Typography { get; init; } = new();
    public LayoutDto Layout { get; init; } = new();
    public bool IsDefault { get; init; }

    public static TenantThemeDto Default => new();
}

[ImmutableObject(true)]
public sealed record PaletteDto
{
    public string Primary { get; init; } = "#2563EB";
    public string Secondary { get; init; } = "#0F172A";
    public string Tertiary { get; init; } = "#6366F1";
    public string Background { get; init; } = "#F8FAFC";
    public string Surface { get; init; } = "#FFFFFF";
    public string Error { get; init; } = "#DC2626";
    public string Warning { get; init; } = "#F59E0B";
    public string Success { get; init; } = "#16A34A";
    public string Info { get; init; } = "#0284C7";

    public static PaletteDto DefaultLight => new();

    public static PaletteDto DefaultDark => new()
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
}

[ImmutableObject(true)]
public sealed record BrandAssetsDto
{
    // Current URLs (returned from API)
    public string? LogoUrl { get; init; }
    public string? LogoDarkUrl { get; init; }
    public string? FaviconUrl { get; init; }

    // File uploads (same pattern as profile picture)
    public FileUploadRequest? Logo { get; init; }
    public FileUploadRequest? LogoDark { get; init; }
    public FileUploadRequest? Favicon { get; init; }

    // Flags to delete current assets
    public bool DeleteLogo { get; init; }
    public bool DeleteLogoDark { get; init; }
    public bool DeleteFavicon { get; init; }
}

[ImmutableObject(true)]
public sealed record TypographyDto
{
    public string FontFamily { get; init; } = "Inter, sans-serif";
    public string HeadingFontFamily { get; init; } = "Inter, sans-serif";
    public double FontSizeBase { get; init; } = 14;
    public double LineHeightBase { get; init; } = 1.5;

    public static IReadOnlyList<string> WebSafeFonts => new[]
    {
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
    };
}

[ImmutableObject(true)]
public sealed record LayoutDto
{
    public string BorderRadius { get; init; } = "4px";
    public int DefaultElevation { get; init; } = 1;
}
