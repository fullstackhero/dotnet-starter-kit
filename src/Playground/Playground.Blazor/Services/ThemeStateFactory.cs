using System.Text.Json;
using FSH.Framework.Blazor.UI.Theme;
using Microsoft.Extensions.Caching.Distributed;

namespace FSH.Playground.Blazor.Services;

/// <summary>
/// Factory for loading theme state, optimized for SSR scenarios.
/// </summary>
internal interface IThemeStateFactory
{
    Task<TenantThemeSettings> GetThemeAsync(string tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis-cached implementation of theme state factory.
/// </summary>
internal sealed class CachedThemeStateFactory : IThemeStateFactory
{
    private static readonly Uri ThemeEndpoint = new("/api/v1/tenants/theme", UriKind.Relative);
    private static readonly TenantThemeSettings DefaultSettings = TenantThemeSettings.Default;

    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CachedThemeStateFactory> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

    public CachedThemeStateFactory(
        IDistributedCache cache,
        HttpClient httpClient,
        ILogger<CachedThemeStateFactory> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TenantThemeSettings> GetThemeAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"theme:{tenantId}";

        var cached = await TryGetFromCacheAsync(cacheKey, tenantId, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        return await FetchAndCacheThemeAsync(cacheKey, tenantId, cancellationToken);
    }

    private async Task<TenantThemeSettings?> TryGetFromCacheAsync(
        string cacheKey,
        string tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (json is null)
            {
                return null;
            }

            return DeserializeTheme(json, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache unavailable, fetching theme directly for tenant {TenantId}", tenantId);
            return null;
        }
    }

    private TenantThemeSettings? DeserializeTheme(string json, string tenantId)
    {
        try
        {
            return JsonSerializer.Deserialize<TenantThemeSettings>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached theme for tenant {TenantId}", tenantId);
            return null;
        }
    }

    private async Task<TenantThemeSettings> FetchAndCacheThemeAsync(
        string cacheKey,
        string tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await FetchThemeFromApiAsync(cancellationToken);
            if (settings is not null)
            {
                await TryCacheThemeAsync(cacheKey, settings, cancellationToken);
                return settings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenant theme for {TenantId}", tenantId);
        }

        return DefaultSettings;
    }

    private async Task<TenantThemeSettings?> FetchThemeFromApiAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(ThemeEndpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to load tenant theme from API: {StatusCode}", response.StatusCode);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<TenantThemeApiDto>(cancellationToken);
        return dto is not null ? MapFromDto(dto) : null;
    }

    private async Task TryCacheThemeAsync(
        string cacheKey,
        TenantThemeSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(settings);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiry
            };
            await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache theme, continuing without cache");
        }
    }

    private static TenantThemeSettings MapFromDto(TenantThemeApiDto dto) => new()
    {
        LightPalette = MapLightPalette(dto.LightPalette),
        DarkPalette = MapDarkPalette(dto.DarkPalette),
        BrandAssets = MapBrandAssets(dto.BrandAssets),
        Typography = MapTypography(dto.Typography),
        Layout = MapLayout(dto.Layout),
        IsDefault = dto.IsDefault
    };

    private static PaletteSettings MapLightPalette(PaletteApiDto? dto) => new()
    {
        Primary = dto?.Primary ?? DefaultSettings.LightPalette.Primary,
        Secondary = dto?.Secondary ?? DefaultSettings.LightPalette.Secondary,
        Tertiary = dto?.Tertiary ?? DefaultSettings.LightPalette.Tertiary,
        Background = dto?.Background ?? DefaultSettings.LightPalette.Background,
        Surface = dto?.Surface ?? DefaultSettings.LightPalette.Surface,
        Error = dto?.Error ?? DefaultSettings.LightPalette.Error,
        Warning = dto?.Warning ?? DefaultSettings.LightPalette.Warning,
        Success = dto?.Success ?? DefaultSettings.LightPalette.Success,
        Info = dto?.Info ?? DefaultSettings.LightPalette.Info
    };

    private static PaletteSettings MapDarkPalette(PaletteApiDto? dto) => new()
    {
        Primary = dto?.Primary ?? DefaultSettings.DarkPalette.Primary,
        Secondary = dto?.Secondary ?? DefaultSettings.DarkPalette.Secondary,
        Tertiary = dto?.Tertiary ?? DefaultSettings.DarkPalette.Tertiary,
        Background = dto?.Background ?? DefaultSettings.DarkPalette.Background,
        Surface = dto?.Surface ?? DefaultSettings.DarkPalette.Surface,
        Error = dto?.Error ?? DefaultSettings.DarkPalette.Error,
        Warning = dto?.Warning ?? DefaultSettings.DarkPalette.Warning,
        Success = dto?.Success ?? DefaultSettings.DarkPalette.Success,
        Info = dto?.Info ?? DefaultSettings.DarkPalette.Info
    };

    private static BrandAssets MapBrandAssets(BrandAssetsApiDto? dto) => new()
    {
        LogoUrl = dto?.LogoUrl,
        LogoDarkUrl = dto?.LogoDarkUrl,
        FaviconUrl = dto?.FaviconUrl
    };

    private static TypographySettings MapTypography(TypographyApiDto? dto) => new()
    {
        FontFamily = dto?.FontFamily ?? DefaultSettings.Typography.FontFamily,
        HeadingFontFamily = dto?.HeadingFontFamily ?? DefaultSettings.Typography.HeadingFontFamily,
        FontSizeBase = dto?.FontSizeBase ?? DefaultSettings.Typography.FontSizeBase,
        LineHeightBase = dto?.LineHeightBase ?? DefaultSettings.Typography.LineHeightBase
    };

    private static LayoutSettings MapLayout(LayoutApiDto? dto) => new()
    {
        BorderRadius = dto?.BorderRadius ?? DefaultSettings.Layout.BorderRadius,
        DefaultElevation = dto?.DefaultElevation ?? DefaultSettings.Layout.DefaultElevation
    };
}
