using FSH.Framework.Blazor.UI.Theme;
using MudBlazor;
using System.Text.Json.Serialization;

namespace FSH.Playground.Blazor.Services;

/// <summary>
/// Implementation of ITenantThemeState that fetches/saves theme settings via the API.
/// </summary>
public sealed class TenantThemeState : ITenantThemeState
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TenantThemeState> _logger;
    private readonly string _apiBaseUrl;

    private TenantThemeSettings _current = TenantThemeSettings.Default;
    private MudTheme _theme;
    private bool _isDarkMode;

    public TenantThemeState(HttpClient httpClient, ILogger<TenantThemeState> logger, IConfiguration configuration)
    {
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
            var response = await _httpClient.GetAsync("/api/v1/tenants/theme", cancellationToken);

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
        var response = await _httpClient.PostAsync("/api/v1/tenants/theme/reset", null, cancellationToken);
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

    public void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
    }

    private TenantThemeSettings MapFromDto(TenantThemeApiDto dto)
    {
        return new TenantThemeSettings
        {
            LightPalette = new PaletteSettings
            {
                Primary = dto.LightPalette?.Primary ?? "#2563EB",
                Secondary = dto.LightPalette?.Secondary ?? "#0F172A",
                Tertiary = dto.LightPalette?.Tertiary ?? "#6366F1",
                Background = dto.LightPalette?.Background ?? "#F8FAFC",
                Surface = dto.LightPalette?.Surface ?? "#FFFFFF",
                Error = dto.LightPalette?.Error ?? "#DC2626",
                Warning = dto.LightPalette?.Warning ?? "#F59E0B",
                Success = dto.LightPalette?.Success ?? "#16A34A",
                Info = dto.LightPalette?.Info ?? "#0284C7"
            },
            DarkPalette = new PaletteSettings
            {
                Primary = dto.DarkPalette?.Primary ?? "#38BDF8",
                Secondary = dto.DarkPalette?.Secondary ?? "#94A3B8",
                Tertiary = dto.DarkPalette?.Tertiary ?? "#818CF8",
                Background = dto.DarkPalette?.Background ?? "#0B1220",
                Surface = dto.DarkPalette?.Surface ?? "#111827",
                Error = dto.DarkPalette?.Error ?? "#F87171",
                Warning = dto.DarkPalette?.Warning ?? "#FBBF24",
                Success = dto.DarkPalette?.Success ?? "#22C55E",
                Info = dto.DarkPalette?.Info ?? "#38BDF8"
            },
            BrandAssets = new BrandAssets
            {
                LogoUrl = ToAbsoluteUrl(dto.BrandAssets?.LogoUrl),
                LogoDarkUrl = ToAbsoluteUrl(dto.BrandAssets?.LogoDarkUrl),
                FaviconUrl = ToAbsoluteUrl(dto.BrandAssets?.FaviconUrl)
            },
            Typography = new TypographySettings
            {
                FontFamily = dto.Typography?.FontFamily ?? "Inter, sans-serif",
                HeadingFontFamily = dto.Typography?.HeadingFontFamily ?? "Inter, sans-serif",
                FontSizeBase = dto.Typography?.FontSizeBase ?? 14,
                LineHeightBase = dto.Typography?.LineHeightBase ?? 1.5
            },
            Layout = new LayoutSettings
            {
                BorderRadius = dto.Layout?.BorderRadius ?? "4px",
                DefaultElevation = dto.Layout?.DefaultElevation ?? 1
            },
            IsDefault = dto.IsDefault
        };
    }

    private string? ToAbsoluteUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return null;

        // Already absolute URL or data URL
        if (relativeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            relativeUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return relativeUrl;
        }

        // Prepend API base URL to relative path
        return $"{_apiBaseUrl}/{relativeUrl}";
    }

    private static TenantThemeApiDto MapToDto(TenantThemeSettings settings)
    {
        return new TenantThemeApiDto
        {
            LightPalette = new PaletteApiDto
            {
                Primary = settings.LightPalette.Primary,
                Secondary = settings.LightPalette.Secondary,
                Tertiary = settings.LightPalette.Tertiary,
                Background = settings.LightPalette.Background,
                Surface = settings.LightPalette.Surface,
                Error = settings.LightPalette.Error,
                Warning = settings.LightPalette.Warning,
                Success = settings.LightPalette.Success,
                Info = settings.LightPalette.Info
            },
            DarkPalette = new PaletteApiDto
            {
                Primary = settings.DarkPalette.Primary,
                Secondary = settings.DarkPalette.Secondary,
                Tertiary = settings.DarkPalette.Tertiary,
                Background = settings.DarkPalette.Background,
                Surface = settings.DarkPalette.Surface,
                Error = settings.DarkPalette.Error,
                Warning = settings.DarkPalette.Warning,
                Success = settings.DarkPalette.Success,
                Info = settings.DarkPalette.Info
            },
            BrandAssets = new BrandAssetsApiDto
            {
                LogoUrl = settings.BrandAssets.LogoUrl,
                LogoDarkUrl = settings.BrandAssets.LogoDarkUrl,
                FaviconUrl = settings.BrandAssets.FaviconUrl,
                Logo = MapFileUpload(settings.BrandAssets.Logo),
                LogoDark = MapFileUpload(settings.BrandAssets.LogoDark),
                Favicon = MapFileUpload(settings.BrandAssets.Favicon),
                DeleteLogo = settings.BrandAssets.DeleteLogo,
                DeleteLogoDark = settings.BrandAssets.DeleteLogoDark,
                DeleteFavicon = settings.BrandAssets.DeleteFavicon
            },
            Typography = new TypographyApiDto
            {
                FontFamily = settings.Typography.FontFamily,
                HeadingFontFamily = settings.Typography.HeadingFontFamily,
                FontSizeBase = settings.Typography.FontSizeBase,
                LineHeightBase = settings.Typography.LineHeightBase
            },
            Layout = new LayoutApiDto
            {
                BorderRadius = settings.Layout.BorderRadius,
                DefaultElevation = settings.Layout.DefaultElevation
            },
            IsDefault = settings.IsDefault
        };
    }

    private static FileUploadApiDto? MapFileUpload(FileUpload? upload)
    {
        if (upload is null || upload.Data.Length == 0)
            return null;

        // Convert byte[] to List<int> for JSON serialization (same as profile picture pattern)
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

    // File upload data (same pattern as profile picture)
    [JsonPropertyName("logo")]
    public FileUploadApiDto? Logo { get; init; }

    [JsonPropertyName("logoDark")]
    public FileUploadApiDto? LogoDark { get; init; }

    [JsonPropertyName("favicon")]
    public FileUploadApiDto? Favicon { get; init; }

    // Delete flags
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
