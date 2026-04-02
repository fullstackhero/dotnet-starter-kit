using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Shared.Localization;

/// <summary>
/// Extension methods to wire up localization in both the API and Blazor hosts.
/// Supports modular localization architecture:
/// - SharedResource: Common strings used across all modules (Email, Password, Required, etc.)
/// - Module-specific resources: Each module has its own resource file (e.g., IdentityResource, TenantResource)
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Registers localization services for modular architecture. Call in IServiceCollection setup.
    /// </summary>
    /// <remarks>
    /// Resources are discovered based on namespace structure:
    /// - FSH.Framework.Shared.Localization.SharedResource → Shared/Localization/SharedResource.resx
    /// - FSH.Modules.Identity.Localization.IdentityResource → Identity/Localization/IdentityResource.resx
    /// - FSH.Modules.Multitenancy.Localization.TenantResource → Multitenancy/Localization/TenantResource.resx
    /// 
    /// Usage in components:
    /// @inject IStringLocalizer&lt;SharedResource&gt; L      // Common strings
    /// @inject IStringLocalizer&lt;IdentityResource&gt; IL  // Identity-specific strings
    /// </remarks>
    public static IServiceCollection AddFshLocalization(this IServiceCollection services)
    {
        // Register localization without specifying ResourcesPath
        // Resources are discovered based on the namespace structure
        // This allows each module to have its own resource files
        services.AddLocalization();
        return services;
    }

    /// <summary>
    /// Adds request localization middleware using <see cref="LocalizationConstants.SupportedCultures"/>.
    /// Culture is resolved in order: cookie → Accept-Language header → default.
    /// </summary>
    public static IApplicationBuilder UseFshLocalization(this IApplicationBuilder app)
    {
        var options = new RequestLocalizationOptions()
            .SetDefaultCulture(LocalizationConstants.DefaultCulture)
            .AddSupportedCultures(LocalizationConstants.SupportedCultures)
            .AddSupportedUICultures(LocalizationConstants.SupportedCultures);

        // Cookie provider allows per-user culture selection
        options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider
        {
            CookieName = LocalizationConstants.CultureCookieName
        });

        app.UseRequestLocalization(options);
        return app;
    }
}
