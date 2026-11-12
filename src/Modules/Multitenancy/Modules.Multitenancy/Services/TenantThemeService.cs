using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Services;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Domain;
using FSH.Modules.Multitenancy.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Multitenancy.Services;

public sealed class TenantThemeService : ITenantThemeService
{
    private static readonly HybridCacheEntryOptions ThemeEntryOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
    };

    private static readonly string[] DefaultThemeTags = [CacheKeys.Tags.Themes];

    private readonly HybridCache _cache;
    private readonly TenantDbContext _dbContext;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly IStorageService _storageService;
    private readonly ILogger<TenantThemeService> _logger;
    private readonly ICurrentUser _currentUser;

    public TenantThemeService(
        HybridCache cache,
        TenantDbContext dbContext,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        IStorageService storageService,
        ILogger<TenantThemeService> logger,
        ICurrentUser currentUser)
    {
        _cache = cache;
        _dbContext = dbContext;
        _tenantAccessor = tenantAccessor;
        _storageService = storageService;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TenantThemeDto> GetCurrentTenantThemeAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new InvalidOperationException("No tenant context available");
        return await GetThemeAsync(tenantId, ct).ConfigureAwait(false);
    }

    public Task<TenantThemeDto> GetThemeAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // Per-tenant tag array — one small alloc per call is unavoidable because the tag is
        // parameterized by tenantId. Keeping the array allocation local (not LOH) and short-lived.
        var tags = new[] { CacheKeys.Tags.Themes, CacheKeys.Tags.Tenant(tenantId) };

        // Stateless factory via a static method group — no closure allocation even on L1 hits.
        var state = new TenantFactoryState(_dbContext, tenantId);
        return _cache.GetOrCreateAsync(
            CacheKeys.TenantTheme(tenantId),
            state,
            LoadTenantThemeAsync,
            ThemeEntryOptions,
            tags,
            ct).AsTask();
    }

    public Task<TenantThemeDto> GetDefaultThemeAsync(CancellationToken ct = default)
    {
        return _cache.GetOrCreateAsync(
            CacheKeys.DefaultTheme,
            _dbContext,
            LoadDefaultThemeAsync,
            ThemeEntryOptions,
            DefaultThemeTags,
            ct).AsTask();
    }

    private static async ValueTask<TenantThemeDto> LoadTenantThemeAsync(TenantFactoryState state, CancellationToken ct)
    {
        var entity = await state.DbContext.TenantThemes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == state.TenantId, ct)
            .ConfigureAwait(false);

        return entity is null ? TenantThemeDto.Default : MapEntityToDto(entity);
    }

    private static async ValueTask<TenantThemeDto> LoadDefaultThemeAsync(TenantDbContext dbContext, CancellationToken ct)
    {
        var entity = await dbContext.TenantThemes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsDefault, ct)
            .ConfigureAwait(false);

        return entity is null ? TenantThemeDto.Default : MapEntityToDto(entity);
    }

    private readonly record struct TenantFactoryState(TenantDbContext DbContext, string TenantId);

    public async Task UpdateThemeAsync(string tenantId, TenantThemeDto theme, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(theme);

        var entity = await _dbContext.TenantThemes
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = TenantTheme.Create(tenantId);
            _dbContext.TenantThemes.Add(entity);
        }

        // Handle brand asset uploads
        var assetChanges = await HandleBrandAssetUploadsAsync(theme.BrandAssets, entity, ct).ConfigureAwait(false);

        MapDtoToEntity(theme, entity);
        AdoptClientBrandAssetUrls(theme.BrandAssets, entity, assetChanges);
        entity.Update(GetCurrentUserId());

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await InvalidateCacheAsync(tenantId, ct).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Updated theme for tenant {TenantId}", tenantId);
        }
    }

    /// <summary>Which brand assets this request uploaded or deleted, so their URLs are not overwritten.</summary>
    private readonly record struct BrandAssetChanges(bool Logo, bool LogoDark, bool Favicon);

    private async Task<BrandAssetChanges> HandleBrandAssetUploadsAsync(BrandAssetsDto assets, TenantTheme entity, CancellationToken ct)
    {
        bool logo = await ApplyBrandAssetAsync(assets.Logo, assets.DeleteLogo, entity.LogoUrl, url => entity.LogoUrl = url, ct).ConfigureAwait(false);
        bool logoDark = await ApplyBrandAssetAsync(assets.LogoDark, assets.DeleteLogoDark, entity.LogoDarkUrl, url => entity.LogoDarkUrl = url, ct).ConfigureAwait(false);
        bool favicon = await ApplyBrandAssetAsync(assets.Favicon, assets.DeleteFavicon, entity.FaviconUrl, url => entity.FaviconUrl = url, ct).ConfigureAwait(false);
        return new BrandAssetChanges(logo, logoDark, favicon);
    }

    /// <summary>Uploads a replacement or deletes the asset; true when the stored URL changed.</summary>
    private async Task<bool> ApplyBrandAssetAsync(
        FileUploadRequest? upload, bool delete, string? currentUrl, Action<string?> setUrl, CancellationToken ct)
    {
        if (upload?.Data is { Count: > 0 })
        {
            setUrl(await _storageService.UploadAsync<TenantTheme>(upload, FileType.Image, ct).ConfigureAwait(false));
            await RemoveOwnedUploadAsync(currentUrl, ct).ConfigureAwait(false);
            return true;
        }

        if (delete && !string.IsNullOrEmpty(currentUrl))
        {
            await RemoveOwnedUploadAsync(currentUrl, ct).ConfigureAwait(false);
            setUrl(null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Deletes a replaced asset only when this service uploaded it. A stored URL can also be one the
    /// client typed in (an externally hosted logo), and handing that to the storage layer would let a
    /// tenant administrator delete objects — or, with local storage, files — that are not theirs.
    /// </summary>
    private Task RemoveOwnedUploadAsync(string? url, CancellationToken ct) =>
        StorageUploadKeys.IsUploadOf<TenantTheme>(url)
            ? _storageService.RemoveAsync(url!, ct)
            : Task.CompletedTask;

    /// <summary>
    /// Takes the logo URLs the client sent (an externally hosted image, or the current value echoed
    /// back). An asset uploaded or deleted in this request keeps the URL that operation produced, and a
    /// client URL naming another stored upload is ignored: adopting it would make the next delete
    /// remove that object.
    /// </summary>
    private void AdoptClientBrandAssetUrls(BrandAssetsDto dto, TenantTheme entity, BrandAssetChanges changed)
    {
        if (!changed.Logo)
        {
            entity.LogoUrl = AdoptClientUrl(dto.LogoUrl, entity.LogoUrl, nameof(dto.LogoUrl));
        }

        if (!changed.LogoDark)
        {
            entity.LogoDarkUrl = AdoptClientUrl(dto.LogoDarkUrl, entity.LogoDarkUrl, nameof(dto.LogoDarkUrl));
        }

        if (!changed.Favicon)
        {
            entity.FaviconUrl = AdoptClientUrl(dto.FaviconUrl, entity.FaviconUrl, nameof(dto.FaviconUrl));
        }
    }

    private string? AdoptClientUrl(string? candidate, string? current, string asset)
    {
        // Empty keeps the current value, and a data: URL is the editor's local preview, not a URL.
        if (string.IsNullOrEmpty(candidate)
            || candidate.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate, current, StringComparison.Ordinal))
        {
            return current;
        }

        if (StorageUploadKeys.IsUploadOf<TenantTheme>(candidate))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Ignored a client-supplied {Asset} that points at another stored upload", asset);
            }

            return current;
        }

        return candidate;
    }

    public async Task ResetThemeAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var entity = await _dbContext.TenantThemes
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            entity.ResetToDefaults();
            entity.Update(GetCurrentUserId());
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        await InvalidateCacheAsync(tenantId, ct).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Reset theme to defaults for tenant {TenantId}", tenantId);
        }
    }

    public async Task SetAsDefaultThemeAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // Ensure only root tenant can set default theme
        var currentTenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (currentTenantId != MultitenancyConstants.Root.Id)
        {
            throw new ForbiddenException("Only the root tenant can set the default theme")
                .Localized<MultitenancyResources>("Multitenancy.OnlyRootCanSetDefaultTheme");
        }

        // Clear existing default
        var existingDefault = await _dbContext.TenantThemes
            .FirstOrDefaultAsync(t => t.IsDefault, ct)
            .ConfigureAwait(false);

        if (existingDefault is not null)
        {
            existingDefault.IsDefault = false;
        }

        // Set new default
        var entity = await _dbContext.TenantThemes
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new NotFoundException($"Theme for tenant {tenantId} not found")
                .Localized<MultitenancyResources>("Multitenancy.TenantThemeNotFound", tenantId);
        }

        entity.IsDefault = true;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        // Invalidate default theme cache
        await _cache.RemoveAsync(CacheKeys.DefaultTheme, ct).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Set theme for tenant {TenantId} as default", tenantId);
        }
    }

    public async Task InvalidateCacheAsync(string tenantId, CancellationToken ct = default)
    {
        // Purge both the tenant-specific entry and anything tagged for this tenant.
        await _cache.RemoveAsync(CacheKeys.TenantTheme(tenantId), ct).ConfigureAwait(false);
        await _cache.RemoveByTagAsync(CacheKeys.Tags.Tenant(tenantId), ct).ConfigureAwait(false);
    }

    private static TenantThemeDto MapEntityToDto(TenantTheme entity)
    {
        return new TenantThemeDto
        {
            LightPalette = new PaletteDto
            {
                Primary = entity.PrimaryColor,
                Secondary = entity.SecondaryColor,
                Tertiary = entity.TertiaryColor,
                Background = entity.BackgroundColor,
                Surface = entity.SurfaceColor,
                Error = entity.ErrorColor,
                Warning = entity.WarningColor,
                Success = entity.SuccessColor,
                Info = entity.InfoColor
            },
            DarkPalette = new PaletteDto
            {
                Primary = entity.DarkPrimaryColor,
                Secondary = entity.DarkSecondaryColor,
                Tertiary = entity.DarkTertiaryColor,
                Background = entity.DarkBackgroundColor,
                Surface = entity.DarkSurfaceColor,
                Error = entity.DarkErrorColor,
                Warning = entity.DarkWarningColor,
                Success = entity.DarkSuccessColor,
                Info = entity.DarkInfoColor
            },
            BrandAssets = new BrandAssetsDto
            {
                LogoUrl = entity.LogoUrl,
                LogoDarkUrl = entity.LogoDarkUrl,
                FaviconUrl = entity.FaviconUrl
            },
            Typography = new TypographyDto
            {
                FontFamily = entity.FontFamily,
                HeadingFontFamily = entity.HeadingFontFamily,
                FontSizeBase = entity.FontSizeBase,
                LineHeightBase = entity.LineHeightBase
            },
            Layout = new LayoutDto
            {
                BorderRadius = entity.BorderRadius,
                DefaultElevation = entity.DefaultElevation
            },
            IsDefault = entity.IsDefault
        };
    }

    private static void MapDtoToEntity(TenantThemeDto dto, TenantTheme entity)
    {
        // Light Palette
        entity.PrimaryColor = dto.LightPalette.Primary;
        entity.SecondaryColor = dto.LightPalette.Secondary;
        entity.TertiaryColor = dto.LightPalette.Tertiary;
        entity.BackgroundColor = dto.LightPalette.Background;
        entity.SurfaceColor = dto.LightPalette.Surface;
        entity.ErrorColor = dto.LightPalette.Error;
        entity.WarningColor = dto.LightPalette.Warning;
        entity.SuccessColor = dto.LightPalette.Success;
        entity.InfoColor = dto.LightPalette.Info;

        // Dark Palette
        entity.DarkPrimaryColor = dto.DarkPalette.Primary;
        entity.DarkSecondaryColor = dto.DarkPalette.Secondary;
        entity.DarkTertiaryColor = dto.DarkPalette.Tertiary;
        entity.DarkBackgroundColor = dto.DarkPalette.Background;
        entity.DarkSurfaceColor = dto.DarkPalette.Surface;
        entity.DarkErrorColor = dto.DarkPalette.Error;
        entity.DarkWarningColor = dto.DarkPalette.Warning;
        entity.DarkSuccessColor = dto.DarkPalette.Success;
        entity.DarkInfoColor = dto.DarkPalette.Info;

        // Brand asset URLs are applied by HandleBrandAssetUploadsAsync and AdoptClientBrandAssetUrls.

        // Typography
        entity.FontFamily = dto.Typography.FontFamily;
        entity.HeadingFontFamily = dto.Typography.HeadingFontFamily;
        entity.FontSizeBase = dto.Typography.FontSizeBase;
        entity.LineHeightBase = dto.Typography.LineHeightBase;

        // Layout
        entity.BorderRadius = dto.Layout.BorderRadius;
        entity.DefaultElevation = dto.Layout.DefaultElevation;
    }

    private string? GetCurrentUserId()
    {
        var userId = _currentUser.GetUserId();
        return userId == Guid.Empty ? null : userId.ToString();
    }
}