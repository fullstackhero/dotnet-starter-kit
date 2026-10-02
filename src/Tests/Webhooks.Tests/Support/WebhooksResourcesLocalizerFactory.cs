using FSH.Modules.Webhooks.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Webhooks.Tests.Support;

// Builds a REAL IStringLocalizer<WebhooksResources> bound to the embedded resx catalog
// (ResourcesPath="" — co-located marker + resx) so validators that require a localizer can be
// instantiated in unit tests exercising the actual catalog rather than a stub.
internal static class WebhooksResourcesLocalizerFactory
{
    public static IStringLocalizer<WebhooksResources> Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<WebhooksResources>>();
    }
}
