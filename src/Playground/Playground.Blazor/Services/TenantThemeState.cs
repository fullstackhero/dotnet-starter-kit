using FSH.Framework.Blazor.UI.Theme;
using MudBlazor;
using System.Text.Json.Serialization;

namespace FSH.Playground.Blazor.Services;

/// <summary>
/// Implementation of ITenantThemeState that fetches/saves theme settings via the API.
/// </summary>
internal sealed class TenantThemeState : ITenantThemeState
{
    private static readonly Uri ThemeEndpoint = new("/api/v1/tenants/theme", UriKind.Relative);
    private static readonly Uri ThemeResetEndpoint = new("/api/v1/tenants/theme/reset", UriKind.Relative);

    private readonly HttpClient _httpClient;
    private readonly ILogger<TenantThemeState> _logger;
    private readonly string _apiBaseUrl;

    private TenantThemeSettings _current = TenantThemeSettings.Default;
    private MudTheme _theme;
    private bool _isDarkMode;

    public TenantThemeState(HttpClient httpClient, ILogger<TenantThemeState> logger, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _httpClient = httpClient;
        _logger = logger;
        _apiBaseUrl = configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        _theme = _current.ToMudTheme();
    }

    public TenantThemeSettings Current => _current;
    public MudTheme Theme => _theme;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnThemeChanged?.Invoke();
            }
        }
    }

    public event Action? OnThemeChanged;

    public async Task LoadThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(ThemeEndpoint, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var dto = await response.Content.ReadFromJsonAsync<TenantThemeApiDto>(cancellationToken);
                if (dto is not null)
                {
                    _current = MapFromDto(dto);
                    _theme = _current.ToMudTheme();
                    OnThemeChanged?.Invoke();
                }
            }
            else
            {
                _logger.LogWarning("Failed to load tenant theme: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenant theme");
        }
    }

    public async Task SaveThemeAsync(CancellationToken cancellationToken = default)
    {
        var dto = MapToDto(_current);
        var response = await _httpClient.PutAsJsonAsync("/api/v1/tenants/theme", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        _theme = _current.ToMudTheme();
        OnThemeChanged?.Invoke();
    }

    public async Task ResetThemeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(ThemeResetEndpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        _current = TenantThemeSettings.Default;
        _theme = _current.ToMudTheme();
        OnThemeChanged?.Invoke();
    }

    public void UpdateSettings(TenantThemeSettings settings)
    {
        _current = settings;
        _theme = _current.ToMudTheme();
        OnThemeChanged?.Invoke();
    }

    public void ToggleDarkMode() => IsDarkMode = !IsDarkMode;

    private TenantThemeSettings MapFromDto(TenantThemeApiDto dto)
    {
        return new TenantThemeSettings
        {
            LightPalette = MapLightPalette(dto.LightPalette),
            DarkPalette = MapDarkPalette(dto.DarkPalette),
            BrandAssets = MapBrandAssets(dto.BrandAssets),
            Typography = MapTypography(dto.Typography),
            Layout = MapLayout(dto.Layout),
            IsDefault = dto.IsDefault
        };
    }

    private static PaletteSettings MapLightPalette(PaletteApiDto? dto) => new()
    {
        Primary = dto?.Primary ?? "#2563EB",
        Secondary = dto?.Secondary ?? "#0F172A",
        Tertiary = dto?.Tertiary ?? "#6366F1",
        Background = dto?.Background ?? "#F8FAFC",
        Surface = dto?.Surface ?? "#FFFFFF",
        Error = dto?.Error ?? "#DC2626",
        Warning = dto?.Warning ?? "#F59E0B",
        Success = dto?.Success ?? "#16A34A",
        Info = dto?.Info ?? "#0284C7"
    };

    private static PaletteSettings MapDarkPalette(PaletteApiDto? dto) => new()
    {
        Primary = dto?.Primary ?? "#38BDF8",
        Secondary = dto?.Secondary ?? "#94A3B8",
        Tertiary = dto?.Tertiary ?? "#818CF8",
        Background = dto?.Background ?? "#0B1220",
        Surface = dto?.Surface ?? "#111827",
        Error = dto?.Error ?? "#F87171",
        Warning = dto?.Warning ?? "#FBBF24",
        Success = dto?.Success ?? "#22C55E",
        Info = dto?.Info ?? "#38BDF8"
    };

    private BrandAssets MapBrandAssets(BrandAssetsApiDto? dto) => new()
    {
        LogoUrl = ToAbsoluteUrl(dto?.LogoUrl),
        LogoDarkUrl = ToAbsoluteUrl(dto?.LogoDarkUrl),
        FaviconUrl = ToAbsoluteUrl(dto?.FaviconUrl)
    };

    private static TypographySettings MapTypography(TypographyApiDto? dto) => new()
    {
        FontFamily = dto?.FontFamily ?? "Inter, sans-serif",
        HeadingFontFamily = dto?.HeadingFontFamily ?? "Inter, sans-serif",
        FontSizeBase = dto?.FontSizeBase ?? 14,
        LineHeightBase = dto?.LineHeightBase ?? 1.5
    };

    private static LayoutSettings MapLayout(LayoutApiDto? dto) => new()
    {
        BorderRadius = dto?.BorderRadius ?? "4px",
        DefaultElevation = dto?.DefaultElevation ?? 1
    };

    private string? ToAbsoluteUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return null;

        if (IsAbsoluteUrl(relativeUrl))
            return relativeUrl;

        return $"{_apiBaseUrl}/{relativeUrl}";
    }

    private static bool IsAbsoluteUrl(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static TenantThemeApiDto MapToDto(TenantThemeSettings settings) => new()
    {
        LightPalette = MapPaletteToDto(settings.LightPalette),
        DarkPalette = MapPaletteToDto(settings.DarkPalette),
        BrandAssets = MapBrandAssetsToDto(settings.BrandAssets),
        Typography = MapTypographyToDto(settings.Typography),
        Layout = MapLayoutToDto(settings.Layout),
        IsDefault = settings.IsDefault
    };

    private static PaletteApiDto MapPaletteToDto(PaletteSettings palette) => new()
    {
        Primary = palette.Primary,
        Secondary = palette.Secondary,
        Tertiary = palette.Tertiary,
        Background = palette.Background,
        Surface = palette.Surface,
        Error = palette.Error,
        Warning = palette.Warning,
        Success = palette.Success,
        Info = palette.Info
    };

    private static BrandAssetsApiDto MapBrandAssetsToDto(BrandAssets assets) => new()
    {
        LogoUrl = assets.LogoUrl,
        LogoDarkUrl = assets.LogoDarkUrl,
        FaviconUrl = assets.FaviconUrl,
        Logo = MapFileUpload(assets.Logo),
        LogoDark = MapFileUpload(assets.LogoDark),
        Favicon = MapFileUpload(assets.Favicon),
        DeleteLogo = assets.DeleteLogo,
        DeleteLogoDark = assets.DeleteLogoDark,
        DeleteFavicon = assets.DeleteFavicon
    };

    private static TypographyApiDto MapTypographyToDto(TypographySettings typography) => new()
    {
        FontFamily = typography.FontFamily,
        HeadingFontFamily = typography.HeadingFontFamily,
        FontSizeBase = typography.FontSizeBase,
        LineHeightBase = typography.LineHeightBase
    };

    private static LayoutApiDto MapLayoutToDto(LayoutSettings layout) => new()
    {
        BorderRadius = layout.BorderRadius,
        DefaultElevation = layout.DefaultElevation
    };

    private static FileUploadApiDto? MapFileUpload(FileUpload? upload)
    {
        if (upload is null || upload.Data.Length == 0)
            return null;

        return new FileUploadApiDto
        {
            FileName = upload.FileName,
            ContentType = upload.ContentType,
            Data = upload.Data.Select(static b => (int)b).ToList()
        };
    }
}

// API DTOs for serialization
internal sealed record TenantThemeApiDto
{
    [JsonPropertyName("lightPalette")]
    public PaletteApiDto? LightPalette { get; init; }

    [JsonPropertyName("darkPalette")]
    public PaletteApiDto? DarkPalette { get; init; }

    [JsonPropertyName("brandAssets")]
    public BrandAssetsApiDto? BrandAssets { get; init; }

    [JsonPropertyName("typography")]
    public TypographyApiDto? Typography { get; init; }

    [JsonPropertyName("layout")]
    public LayoutApiDto? Layout { get; init; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; init; }
}

internal sealed record PaletteApiDto
{
    [JsonPropertyName("primary")]
    public string? Primary { get; init; }

    [JsonPropertyName("secondary")]
    public string? Secondary { get; init; }

    [JsonPropertyName("tertiary")]
    public string? Tertiary { get; init; }

    [JsonPropertyName("background")]
    public string? Background { get; init; }

    [JsonPropertyName("surface")]
    public string? Surface { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("warning")]
    public string? Warning { get; init; }

    [JsonPropertyName("success")]
    public string? Success { get; init; }

    [JsonPropertyName("info")]
    public string? Info { get; init; }
}

internal sealed record BrandAssetsApiDto
{
    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }

    [JsonPropertyName("logoDarkUrl")]
    public string? LogoDarkUrl { get; init; }

    [JsonPropertyName("faviconUrl")]
    public string? FaviconUrl { get; init; }

    [JsonPropertyName("logo")]
    public FileUploadApiDto? Logo { get; init; }

    [JsonPropertyName("logoDark")]
    public FileUploadApiDto? LogoDark { get; init; }

    [JsonPropertyName("favicon")]
    public FileUploadApiDto? Favicon { get; init; }

    [JsonPropertyName("deleteLogo")]
    public bool DeleteLogo { get; init; }

    [JsonPropertyName("deleteLogoDark")]
    public bool DeleteLogoDark { get; init; }

    [JsonPropertyName("deleteFavicon")]
    public bool DeleteFavicon { get; init; }
}

internal sealed record FileUploadApiDto
{
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = default!;

    [JsonPropertyName("contentType")]
    public string ContentType { get; init; } = default!;

    [JsonPropertyName("data")]
    public ICollection<int> Data { get; init; } = [];
}

internal sealed record TypographyApiDto
{
    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; init; }

    [JsonPropertyName("headingFontFamily")]
    public string? HeadingFontFamily { get; init; }

    [JsonPropertyName("fontSizeBase")]
    public double FontSizeBase { get; init; }

    [JsonPropertyName("lineHeightBase")]
    public double LineHeightBase { get; init; }
}

internal sealed record LayoutApiDto
{
    [JsonPropertyName("borderRadius")]
    public string? BorderRadius { get; init; }

    [JsonPropertyName("defaultElevation")]
    public int DefaultElevation { get; init; }
}
