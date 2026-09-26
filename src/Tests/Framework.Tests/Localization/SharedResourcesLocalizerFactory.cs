using FSH.Framework.Core.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Framework.Tests.Localization;

// Builds a REAL IStringLocalizer<SharedResources> bound to the embedded resx catalog
// (ResourcesPath="" — co-located marker + resx). Shared by the handler and validator tests
// so they exercise the actual catalog resolution rather than a stub.
internal static class SharedResourcesLocalizerFactory
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "");
        return services.BuildServiceProvider();
    }

    public static IStringLocalizer<SharedResources> Create() =>
        BuildProvider().GetRequiredService<IStringLocalizer<SharedResources>>();

    // The GlobalExceptionHandler resolves module-catalog keys through IStringLocalizerFactory
    // (via CustomException.ResourceSource); tests build a real factory bound to the same setup.
    public static IStringLocalizerFactory CreateFactory() =>
        BuildProvider().GetRequiredService<IStringLocalizerFactory>();
}
