using FSH.Modules.Auditing.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Generic.Tests.Support;

// Builds a REAL IStringLocalizer<AuditingResources> bound to the embedded resx catalog
// (ResourcesPath="" — co-located marker + resx) so the Auditing validators that require the module
// localizer can be instantiated in unit tests exercising the actual catalog rather than a stub.
internal static class AuditingResourcesLocalizerFactory
{
    public static IStringLocalizer<AuditingResources> Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<AuditingResources>>();
    }
}
