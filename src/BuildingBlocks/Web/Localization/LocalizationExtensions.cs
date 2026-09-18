using System.Globalization;
using FSH.Framework.Core.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Web.Localization;

public static class LocalizationExtensions
{
    /// <summary>
    /// Registers request localization: resx-backed <c>IStringLocalizer</c> and a UI-culture-provider
    /// chain of Query → user <c>locale</c> claim → Accept-Language → configured default → en-US.
    /// The per-deployment default is read from <c>LocalizationOptions:DefaultCulture</c> and validated
    /// against the whitelist, so garbage config falls back to the guaranteed default culture.
    /// Negotiation drives <see cref="CultureInfo.CurrentUICulture"/> only —
    /// <see cref="CultureInfo.CurrentCulture"/> stays invariant, so no request can shift numeric,
    /// date or string formatting anywhere in the pipeline.
    /// </summary>
    public static IServiceCollection AddHeroLocalization(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration["LocalizationOptions:DefaultCulture"];
        var defaultCulture = SupportedCultures.Tags.Contains(configured!) ? configured! : SupportedCultures.Default;

        // ResourcesPath = "" because SharedResources and its resx live in the same folder/namespace
        // (co-located). A non-empty path would double the prefix and IStringLocalizer would silently
        // fall back to the raw key. The resx-resolution test guards this value.
        services.AddLocalization(o => o.ResourcesPath = "");

        services.Configure<RequestLocalizationOptions>(o =>
        {
            // UI-culture-only. RequestLocalizationMiddleware.SetCurrentThreadCulture assigns BOTH
            // CurrentCulture and CurrentUICulture unconditionally, so there is no "leave formatting
            // alone" switch: the culture half has to be pinned instead. Two things make that work.
            //   1. DefaultRequestCulture carries the pair, and the middleware resolves the culture half
            //      as `cultureInfo ??= DefaultRequestCulture.Culture`. Pinning that half to invariant
            //      makes invariant the only value CurrentCulture can ever take.
            //   2. SupportedCultures = null makes the middleware skip culture filtering entirely.
            //      A one-element [InvariantCulture] list would behave the same but log
            //      `UnsupportedCultures` on EVERY request: the middleware's parent-culture walk bails
            //      out at the empty culture name, so invariant is unmatchable by design.
            // An API that emits JSON has no business shifting ToString()/Parse() per request; both
            // React apps already format at the presentation layer. See #1344 review.
            o.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture, new CultureInfo(defaultCulture));
            o.SupportedCultures = null;
            o.AddSupportedUICultures(SupportedCultures.Tags);
            o.ApplyCurrentCultureToResponseHeaders = true;

            // Default order is [Query(0), Cookie(1), AcceptLanguage(2)]. Drop the cookie provider by
            // type (order-independent, so a framework reshuffle of the defaults can't silently remove
            // the wrong provider) and insert the user-claim provider right after query, so the final
            // chain is Query → UserLocaleClaim → AcceptLanguage → configured default → en-US neutral resx.
            var cookieProvider = o.RequestCultureProviders
                .FirstOrDefault(p => p is CookieRequestCultureProvider);
            if (cookieProvider is not null)
            {
                o.RequestCultureProviders.Remove(cookieProvider);
            }

            o.RequestCultureProviders.Insert(1, new UserLocaleRequestCultureProvider());
        });

        return services;
    }

    public static IApplicationBuilder UseHeroLocalization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseRequestLocalization();
    }
}
