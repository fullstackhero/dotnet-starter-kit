using FSH.Framework.Core.Exceptions;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Audit.RealExceptionType reports a localization wrapper as its base type, so an audit query
/// filtering on exceptionType keeps matching the BCL exception after an endpoint is localized.
/// That single step up is only correct while the wrappers are leaves: a subclass of one would
/// report the wrapper itself, and the same BCL exception would show up under two names.
/// The rule is cheaper to enforce than the walk is to write, so it is enforced here.
/// </summary>
public sealed class LocalizedExceptionShapeTests
{
    [Fact]
    public void Every_localization_wrapper_around_a_BCL_exception_is_sealed()
    {
        var assemblies = ModuleAssemblyDiscovery.GetModuleAssemblies()
            .Append(typeof(ILocalizableMessage).Assembly)
            .Distinct()
            .ToArray();

        var offenders = assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(typeof(ILocalizableMessage).IsAssignableFrom)
            // CustomException is ours, not a wrapper around a BCL type, and is meant to be derived from.
            .Where(t => !typeof(CustomException).IsAssignableFrom(t))
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName!)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
