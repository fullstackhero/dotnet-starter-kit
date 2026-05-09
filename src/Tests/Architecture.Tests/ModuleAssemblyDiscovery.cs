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
        // Get the directory where the tests are running
        string baseDir = AppContext.BaseDirectory;

        // Scan for FSH.Modules.*.dll files (excluding Contracts)
        var moduleFiles = Directory.GetFiles(baseDir, "FSH.Modules.*.dll")
            .Where(f => !f.EndsWith(".Contracts.dll", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var assemblies = new List<Assembly>();

        foreach (var file in moduleFiles)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(file);
                assemblies.Add(Assembly.Load(assemblyName));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
            {
                // Skip if not a valid .NET assembly or other load error
            }
#pragma warning restore CA1031
        }

        return assemblies
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

        if (assemblies.Length == 0)
        {
            throw new InvalidOperationException(
                "ModuleAssemblyDiscovery found no FSH module assemblies. " +
                "Ensure Architecture.Tests.csproj references at least one Modules.* project.");
        }

        assemblies.ShouldNotBeEmpty();
    }
}
