using FSH.Framework.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Architecture guardrails for the caching layer.
/// These tests prevent developers from accidentally injecting <see cref="HybridCache"/>
/// directly into business module code, which would bypass the automatic per-tenant
/// key scoping provided by <see cref="ITenantCacheService"/>.
///
/// The two-cache pattern:
/// <list type="bullet">
///   <item><see cref="ITenantCacheService"/> — per-tenant scoped, for all business data.</item>
///   <item><see cref="IGlobalCacheService"/> — cross-tenant singleton, for shared system defaults.</item>
/// </list>
/// Neither role requires module code to inject <c>HybridCache</c> directly.
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
    // Rule 1 — No direct HybridCache dependency in any module class
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that no class in any business module injects <see cref="HybridCache"/> directly.
    /// All module code must inject either <see cref="ITenantCacheService"/> (tenant-scoped data)
    /// or <see cref="IGlobalCacheService"/> (intentionally cross-tenant data).
    /// <para>
    /// Note: <c>HybridCacheEntryOptions</c> (a pure data/options type from the same namespace)
    /// is intentionally allowed — it carries no state and is safe to use as a method argument.
    /// We check for the cache service class by its full name, not the namespace.
    /// </para>
    /// </summary>
    [Fact]
    public void ModuleClasses_Should_Not_Depend_On_HybridCache_Directly()
    {
        // We check by fully-qualified class name rather than namespace because:
        // - HybridCache (the abstract class) is what we want to ban from constructors.
        // - HybridCacheEntryOptions (a data record in the same namespace) is legitimately
        //   used in module code as options arguments to ITenantCacheService / IGlobalCacheService.
        const string hybridCacheClass = "Microsoft.Extensions.Caching.Hybrid.HybridCache";

        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn(hybridCacheClass)
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            failingTypes.ShouldBeEmpty(
                $"Module '{assembly.GetName().Name}' contains types that inject HybridCache directly. " +
                $"Use ITenantCacheService for per-tenant data or IGlobalCacheService for " +
                $"cross-tenant shared data. Violating types: {string.Join(", ", failingTypes)}");
        }
    }

    // -------------------------------------------------------------------------
    // Rule 2 — ITenantCacheService is a public interface (DI contract)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ITenantCacheService"/> is a public interface so that
    /// module assemblies can depend on it. If someone accidentally changes its
    /// visibility, module code would break at DI resolution time rather than compile time.
    /// </summary>
    [Fact]
    public void ITenantCacheService_Should_Be_A_Public_Interface()
    {
        var type = typeof(ITenantCacheService);

        type.IsInterface.ShouldBeTrue(
            $"{type.FullName} must be an interface so modules can depend on the abstraction.");

        type.IsPublic.ShouldBeTrue(
            $"{type.FullName} must be public so module assemblies can reference it.");
    }

    // -------------------------------------------------------------------------
    // Rule 3 — IGlobalCacheService is a public interface (DI contract)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IGlobalCacheService"/> is a public interface, enabling
    /// modules that legitimately need cross-tenant caching (e.g. system defaults)
    /// to depend on it explicitly and intentionally.
    /// </summary>
    [Fact]
    public void IGlobalCacheService_Should_Be_A_Public_Interface()
    {
        var type = typeof(IGlobalCacheService);

        type.IsInterface.ShouldBeTrue(
            $"{type.FullName} must be an interface so modules can depend on the abstraction.");

        type.IsPublic.ShouldBeTrue(
            $"{type.FullName} must be public so module assemblies can reference it.");
    }

    // -------------------------------------------------------------------------
    // Rule 4 — Implementations stay internal (prevent module bypass)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the concrete implementations (<c>TenantHybridCache</c> and
    /// <c>GlobalHybridCache</c>) are internal, preventing modules from
    /// newing them up directly and bypassing the DI contract.
    /// </summary>
    [Fact]
    public void CacheService_Implementations_Should_Be_Internal()
    {
        var cachingAssembly = typeof(ITenantCacheService).Assembly;

        foreach (var implName in new[] { "TenantHybridCache", "GlobalHybridCache" })
        {
            var implType = cachingAssembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == implName);

            implType.ShouldNotBeNull(
                $"{implName} implementation must exist in the Caching assembly.");

            implType!.IsPublic.ShouldBeFalse(
                $"{implName} should be internal so module code cannot instantiate it directly. " +
                $"All consumption must go through the interface resolved via DI.");
        }
    }

    // -------------------------------------------------------------------------
    // Rule 5 — Guard: at least one module was scanned (prevents vacuous pass)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Prevents Rule 1 from silently passing with an empty assembly list,
    /// which would make it vacuously true and miss real violations in new modules.
    /// </summary>
    [Fact]
    public void CachingArchitectureTests_Should_HaveAtLeastOneModuleToScan()
    {
        ModuleAssemblies.ShouldNotBeEmpty(
            "At least one module assembly must be loaded for the caching architecture " +
            "rules to be meaningful. Check Architecture.Tests.csproj project references.");
    }
}
