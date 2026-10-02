using FSH.Framework.Core.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Identity.Tests.Support;

// Builds a REAL IStringLocalizer<SharedResources> bound to the embedded resx catalog so validator
// tests exercise the actual catalog under the ambient UI culture (default culture -> neutral English).
internal static class SharedResourcesLocalizerFactory
{
    public static IStringLocalizer<SharedResources> Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResources>>();
    }
}
