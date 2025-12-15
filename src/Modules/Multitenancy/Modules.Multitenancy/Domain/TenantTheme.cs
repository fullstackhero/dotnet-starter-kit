using FSH.Framework.Core.Domain;

namespace FSH.Modules.Multitenancy.Domain;

public class TenantTheme : BaseEntity<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    // Light Palette
    public string PrimaryColor { get; set; } = "#2563EB";
    public string SecondaryColor { get; set; } = "#0F172A";
    public string TertiaryColor { get; set; } = "#6366F1";
    public string BackgroundColor { get; set; } = "#F8FAFC";
    public string SurfaceColor { get; set; } = "#FFFFFF";
    public string ErrorColor { get; set; } = "#DC2626";
    public string WarningColor { get; set; } = "#F59E0B";
    public string SuccessColor { get; set; } = "#16A34A";
    public string InfoColor { get; set; } = "#0284C7";

    // Dark Palette
    public string DarkPrimaryColor { get; set; } = "#38BDF8";
    public string DarkSecondaryColor { get; set; } = "#94A3B8";
    public string DarkTertiaryColor { get; set; } = "#818CF8";
    public string DarkBackgroundColor { get; set; } = "#0B1220";
    public string DarkSurfaceColor { get; set; } = "#111827";
    public string DarkErrorColor { get; set; } = "#F87171";
    public string DarkWarningColor { get; set; } = "#FBBF24";
    public string DarkSuccessColor { get; set; } = "#22C55E";
    public string DarkInfoColor { get; set; } = "#38BDF8";

    // Brand Assets
    public string? LogoUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? FaviconUrl { get; set; }

    // Typography
    public string FontFamily { get; set; } = "Inter, sans-serif";
    public string HeadingFontFamily { get; set; } = "Inter, sans-serif";
    public double FontSizeBase { get; set; } = 14;
    public double LineHeightBase { get; set; } = 1.5;

    // Layout
    public string BorderRadius { get; set; } = "4px";
    public int DefaultElevation { get; set; } = 1;

    // Is Default Theme (for root tenant to set default for new tenants)
    public bool IsDefault { get; set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private TenantTheme() { } // EF Core

    public static TenantTheme Create(string tenantId, string? createdBy = null)
    {
        return new TenantTheme
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedBy = createdBy,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string? modifiedBy)
    {
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void ResetToDefaults()
    {
        // Light Palette
        PrimaryColor = "#2563EB";
        SecondaryColor = "#0F172A";
        TertiaryColor = "#6366F1";
        BackgroundColor = "#F8FAFC";
        SurfaceColor = "#FFFFFF";
        ErrorColor = "#DC2626";
        WarningColor = "#F59E0B";
        SuccessColor = "#16A34A";
        InfoColor = "#0284C7";

        // Dark Palette
        DarkPrimaryColor = "#38BDF8";
        DarkSecondaryColor = "#94A3B8";
        DarkTertiaryColor = "#818CF8";
        DarkBackgroundColor = "#0B1220";
        DarkSurfaceColor = "#111827";
        DarkErrorColor = "#F87171";
        DarkWarningColor = "#FBBF24";
        DarkSuccessColor = "#22C55E";
        DarkInfoColor = "#38BDF8";

        // Brand Assets
        LogoUrl = null;
        LogoDarkUrl = null;
        FaviconUrl = null;

        // Typography
        FontFamily = "Inter, sans-serif";
        HeadingFontFamily = "Inter, sans-serif";
        FontSizeBase = 14;
        LineHeightBase = 1.5;

        // Layout
        BorderRadius = "4px";
        DefaultElevation = 1;
    }
}
