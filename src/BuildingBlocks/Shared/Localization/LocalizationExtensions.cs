using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Shared.Localization;

/// <summary>
/// Extension methods to wire up localization in both the API and Blazor hosts.
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Registers localization services. Call in IServiceCollection setup.
    /// </summary>
    public static IServiceCollection AddFshLocalization(this IServiceCollection services)
    {
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
