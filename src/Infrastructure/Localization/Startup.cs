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
    internal static IServiceCollection AddPOLocalization(this IServiceCollection services)
    {
        services.AddMvc()
        .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
        services.AddPortableObjectLocalization(options => options.ResourcesPath = "Localization");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("en-US"),
                new CultureInfo("en"),
                new CultureInfo("fr-FR"),
                new CultureInfo("fr"),
                new CultureInfo("de"),
                new CultureInfo("de-DE")
            };

            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        services.AddSingleton<ILocalizationFileLocationProvider, FSHPoFileLocationProvider>();
        return services;
    }
}