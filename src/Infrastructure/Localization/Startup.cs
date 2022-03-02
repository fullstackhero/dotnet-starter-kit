using System.Globalization;
using FSH.WebApi.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Localization;

namespace FSH.WebApi.Infrastructure.Localization;

internal static class Startup
{
    internal static IServiceCollection AddPOLocalization(this IServiceCollection services, IConfiguration config)
    {
        var localizationSettings = config.GetSection(nameof(LocalizationSettings)).Get<LocalizationSettings>();

        if (localizationSettings == null) return services;
        if (localizationSettings.EnableLocalization == false) return services;
        if(localizationSettings.ResourcesPath == null) return services;

        services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

        services.AddPortableObjectLocalization(options => options.ResourcesPath = localizationSettings.ResourcesPath);

        services.Configure<RequestLocalizationOptions>(options =>
        {
            if (localizationSettings.SupportedCultures != null)
            {
                var supportedCultures = localizationSettings.SupportedCultures.Select(x => new CultureInfo(x)).ToList<CultureInfo>();

                options.DefaultRequestCulture = new RequestCulture(localizationSettings.DefaultRequestCulture ?? "en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            }
        });

        services.AddSingleton<ILocalizationFileLocationProvider, FSHPoFileLocationProvider>();
        return services;
    }
}