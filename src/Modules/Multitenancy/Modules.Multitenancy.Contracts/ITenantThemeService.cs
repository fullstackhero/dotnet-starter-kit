using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace FSH.Modules.Multitenancy.Contracts;

public interface ITenantThemeService
{
    /// <summary>
    /// Gets the theme for the specified tenant. Falls back to default theme if none exists.
    /// </summary>
    Task<TenantThemeDto> GetThemeAsync(string tenantId, CancellationToken ct = default);

    /// <summary>
    /// Gets the theme for the current tenant context. Falls back to default theme if none exists.
    /// </summary>
    Task<TenantThemeDto> GetCurrentTenantThemeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the default theme (set by root tenant) for new tenants.
    /// </summary>
    Task<TenantThemeDto> GetDefaultThemeAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the theme for the specified tenant.
    /// </summary>
    Task UpdateThemeAsync(string tenantId, TenantThemeDto theme, CancellationToken ct = default);

    /// <summary>
    /// Resets the theme for the specified tenant to defaults.
    /// </summary>
    Task ResetThemeAsync(string tenantId, CancellationToken ct = default);

    /// <summary>
    /// Sets the specified tenant's theme as the default for new tenants (root tenant only).
    /// </summary>
    Task SetAsDefaultThemeAsync(string tenantId, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cached theme for the specified tenant.
    /// </summary>
    Task InvalidateCacheAsync(string tenantId, CancellationToken ct = default);
}
