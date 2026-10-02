using System.Globalization;
using System.Runtime.CompilerServices;

namespace Catalog.Tests.Support;

internal static class TestUiCulture
{
    /// <summary>
    /// Pins the UI culture for the whole test assembly.
    /// </summary>
    /// <remarks>
    /// The validator tests assert the exact English message, and those messages now come from the
    /// embedded resx, which resolves against <see cref="CultureInfo.CurrentUICulture"/>. Left to the
    /// ambient value, the suite asserts a property of the developer's operating system rather than of
    /// the code: green on an English machine, eleven failures on a pt-BR one, and green again on CI.
    /// Pinning here rather than in each test class keeps the assertions readable and covers every
    /// class that compares a localized string.
    /// </remarks>
    [ModuleInitializer]
    internal static void Pin()
    {
        var english = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = english;
        CultureInfo.DefaultThreadCurrentUICulture = english;
    }
}
