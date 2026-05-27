using FSH.Modules.Auditing;
using FSH.Modules.Identity;
using FSH.Modules.Multitenancy;
using System.Reflection;

namespace Generic.Tests.Architecture;

/// <summary>
/// Discovers all FSH module assemblies for use in generic architecture tests.
/// </summary>
internal static class ModuleAssemblyDiscovery
{
    private static readonly Assembly[] _cached = Discover();

    public static Assembly[] GetModuleAssemblies() => _cached;

    private static Assembly[] Discover()
    {
        // Force-load seed assemblies
        _ = typeof(AuditingModule);
        _ = typeof(IdentityModule);
        _ = typeof(MultitenancyModule);

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a =>
            {
                var name = a.GetName().Name ?? string.Empty;
                return name.StartsWith("FSH.Modules.", StringComparison.Ordinal)
                       && !name.EndsWith(".Contracts", StringComparison.Ordinal);
            })
            .OrderBy(a => a.GetName().Name, StringComparer.Ordinal)
            .ToArray();
    }
}
