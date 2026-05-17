using FSH.Framework.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using NetArchTest.Rules;
using System.Reflection;

namespace Architecture.Tests;

/// <summary>
/// Architecture guardrails for the caching layer.
/// These tests prevent developers from accidentally injecting <see cref="HybridCache"/>
/// directly into business module code, which would bypass the automatic per-tenant
/// key scoping provided by <see cref="ITenantCacheService"/>.
///
/// The rule: <b>module assemblies must inject <see cref="ITenantCacheService"/></b>,
/// not <see cref="HybridCache"/> directly. <see cref="HybridCache"/> is allowed only in
/// BuildingBlocks (the implementation layer) and in designated cross-tenant infrastructure.
/// </summary>
public sealed class CachingArchitectureTests
{
    /// <summary>
    /// All FSH module assemblies discovered at test run time.
    /// The list grows automatically as new modules are added to the test project.
    /// </summary>
    private static readonly Assembly[] ModuleAssemblies =
        ModuleAssemblyDiscovery.GetModuleAssemblies();

    // -------------------------------------------------------------------------
    // Rule 1 — No direct HybridCache injection in module constructors
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that no class in any business module has a constructor parameter
    /// of type <see cref="HybridCache"/>. All module code must inject
    /// <see cref="ITenantCacheService"/> so tenant isolation is enforced structurally.
    /// </summary>
    [Fact]
    public void ModuleClasses_Should_Not_Depend_On_HybridCache_Directly()
    {
        // NetArchTest detects HybridCache as a dependency via the assembly's metadata.
        // A constructor injection of HybridCache creates a dependency on its declaring
        // namespace: Microsoft.Extensions.Caching.Hybrid.
        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("Microsoft.Extensions.Caching.Hybrid")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            // TenantThemeService is a known, intentional exception:
            // it holds a secondary HybridCache reference for the cross-tenant DefaultTheme entry.
            // All other violations are real bugs.
            var realViolations = failingTypes
                .Where(t => !t.EndsWith("TenantThemeService", StringComparison.Ordinal))
                .ToList();

            realViolations.ShouldBeEmpty(
                $"Module '{assembly.GetName().Name}' contains types that depend directly on " +
                $"HybridCache. Use ITenantCacheService instead to guarantee per-tenant key scoping. " +
                $"Violating types: {string.Join(", ", realViolations)}");
        }
    }

    // -------------------------------------------------------------------------
    // Rule 2 — ITenantCacheService is registered (DI contract)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="ITenantCacheService"/> type is publicly accessible
    /// from the Caching assembly. If someone accidentally changes its visibility,
    /// module code would break at DI resolution time rather than compile time.
    /// </summary>
    [Fact]
    public void ITenantCacheService_Should_Be_Public()
    {
        var type = typeof(ITenantCacheService);

        type.IsInterface.ShouldBeTrue(
            $"{type.FullName} must be an interface so modules can depend on the abstraction.");

        type.IsPublic.ShouldBeTrue(
            $"{type.FullName} must be public so module assemblies can reference it.");
    }

    // -------------------------------------------------------------------------
    // Rule 3 — TenantHybridCache implementation remains internal (encapsulation)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the concrete <c>TenantHybridCache</c> implementation is internal,
    /// preventing modules from bypassing the interface and newing it up directly.
    /// </summary>
    [Fact]
    public void TenantHybridCache_Implementation_Should_Be_Internal()
    {
        var cachingAssembly = typeof(ITenantCacheService).Assembly;

        var implType = cachingAssembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "TenantHybridCache");

        implType.ShouldNotBeNull(
            "TenantHybridCache implementation type must exist in the Caching assembly.");

        implType!.IsPublic.ShouldBeFalse(
            "TenantHybridCache should be internal so module code cannot instantiate it directly. " +
            "All consumption must go through ITenantCacheService resolved via DI.");
    }

    // -------------------------------------------------------------------------
    // Rule 4 — Guard: at least one module was scanned
    // -------------------------------------------------------------------------

    /// <summary>
    /// Prevents the tenant isolation check from silently passing with an empty assembly list
    /// (which would make rules 1 vacuously true and miss real violations in new modules).
    /// </summary>
    [Fact]
    public void CachingArchitectureTests_Should_HaveAtLeastOneModuleToScan()
    {
        ModuleAssemblies.ShouldNotBeEmpty(
            "At least one module assembly must be loaded for the caching architecture " +
            "rules to be meaningful. Check Architecture.Tests.csproj project references.");
    }
}
