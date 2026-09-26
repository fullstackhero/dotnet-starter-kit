using FSH.Modules.Multitenancy.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Multitenancy.Tests.Support;

// Builds a REAL IStringLocalizer<MultitenancyResources> bound to the embedded resx catalog
// (ResourcesPath="" — co-located marker + resx) so validators that require a localizer can be
// instantiated in unit tests exercising the actual catalog rather than a stub.
internal static class MultitenancyResourcesLocalizerFactory
{
    public static IStringLocalizer<MultitenancyResources> Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<MultitenancyResources>>();
    }
}
