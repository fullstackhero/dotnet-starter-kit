using FSH.Framework.Core.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Generic.Tests.Support;

// Builds a REAL IStringLocalizer<SharedResources> bound to the embedded resx catalog
// (ResourcesPath="" — co-located marker + resx) so validators that require a localizer can be
// instantiated in unit tests exercising the actual catalog rather than a stub.
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
