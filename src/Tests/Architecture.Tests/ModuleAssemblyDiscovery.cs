using FSH.Modules.Auditing;
using FSH.Modules.Identity;
using FSH.Modules.Multitenancy;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Discovers all FSH module assemblies for use in architecture tests.
/// Uses a seed assembly list to ensure the correct AppDomain is loaded,
/// then auto-discovers any additional module assemblies that are loaded.
/// Adding a new module requires only adding its assembly reference to the
/// Architecture.Tests project — no changes to this file are needed.
/// </summary>
internal static class ModuleAssemblyDiscovery
{
    private static readonly Assembly[] _cached = Discover();

    /// <summary>
    /// Returns all loaded FSH module assemblies (excluding Contracts assemblies).
    /// </summary>
    public static Assembly[] GetModuleAssemblies() => _cached;

    private static Assembly[] Discover()
    {
        // Force-load the known module assemblies so they appear in AppDomain.
        // These act as "seed" references — the project must reference them for
        // the tests to have anything to check. New modules are added by adding
        // a ProjectReference to Architecture.Tests.csproj only.
        _ = typeof(AuditingModule);
        _ = typeof(IdentityModule);
        _ = typeof(MultitenancyModule);

        // Enumerate all loaded assemblies that look like FSH module runtime assemblies.
        // We exclude *.Contracts assemblies because those are contract-only and
        // have different dependency rules.
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

/// <summary>
/// Fixture that validates at least one module assembly was discovered.
/// Prevents silent no-op if all module references are accidentally removed.
/// </summary>
public sealed class ModuleAssemblyDiscoveryGuardTests
{
    [Fact]
    public void ModuleAssemblyDiscovery_Should_FindAtLeastOneModule()
    {
        var assemblies = ModuleAssemblyDiscovery.GetModuleAssemblies();

        assemblies.ShouldNotBeEmpty(
            "ModuleAssemblyDiscovery found no FSH module assemblies. " +
            "Ensure Architecture.Tests.csproj references at least one Modules.* project.");
    }
}
