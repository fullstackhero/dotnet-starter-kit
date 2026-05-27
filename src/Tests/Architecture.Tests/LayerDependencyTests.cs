using FSH.Framework.Core;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to enforce the layered dependency flow:
/// Domain → Application → Infrastructure → Presentation
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly[] ModuleAssemblies = ModuleAssemblyDiscovery.GetModuleAssemblies();

    private static readonly Assembly CoreAssembly = typeof(IFshCore).Assembly;

    [Fact]
    public void Core_Should_Not_Depend_On_EntityFramework()
    {
        var result = Types
            .InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        var failingTypes = result.FailingTypeNames ?? [];

        result.IsSuccessful.ShouldBeTrue(
            $"BuildingBlocks.Core should not depend on Entity Framework. " +
            $"Failing types: {string.Join(", ", failingTypes)}");
    }

    [Fact]
    public void Core_Should_Not_Depend_On_AspNetCore()
    {
        var result = Types
            .InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.AspNetCore.Http")
            .GetResult();

        var failingTypes = result.FailingTypeNames ?? [];

        result.IsSuccessful.ShouldBeTrue(
            $"BuildingBlocks.Core should not depend on ASP.NET Core. " +
            $"Failing types: {string.Join(", ", failingTypes)}");
    }

    [Fact]
    public void Domain_Types_Should_Not_Depend_On_Persistence()
    {
        foreach (var module in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(module)
                .That()
                .ResideInNamespaceContaining(".Core.")
                .ShouldNot()
                .HaveDependencyOnAny(
                    ".Persistence.",
                    ".Data.",
                    "Microsoft.EntityFrameworkCore")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Domain types in module '{module.GetName().Name}' should not depend on persistence layer. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Domain_Types_Should_Not_Depend_On_Infrastructure()
    {
        foreach (var module in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(module)
                .That()
                .ResideInNamespaceContaining(".Core.")
                .ShouldNot()
                .HaveDependencyOnAny(
                    ".Infrastructure.",
                    ".Services.",
                    "Microsoft.Extensions.Logging",
                    "Microsoft.Extensions.Options")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Domain types in module '{module.GetName().Name}' should not depend on infrastructure. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Features_Should_Not_Depend_On_AspNetCore_Directly()
    {
        // Features should use Minimal API abstractions from BuildingBlocks.Web,
        // not depend directly on ASP.NET Core internals
        foreach (var module in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(module)
                .That()
                .ResideInNamespaceContaining(".Features.")
                .And()
                .DoNotHaveNameEndingWith("Endpoint")
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.AspNetCore.Http.HttpContext",
                    "Microsoft.AspNetCore.Mvc")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Feature handlers/validators in module '{module.GetName().Name}' should not depend directly on ASP.NET Core. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }
}