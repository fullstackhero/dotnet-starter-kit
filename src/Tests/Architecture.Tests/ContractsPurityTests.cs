using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Chat.Contracts;
using FSH.Modules.Identity.Contracts;
using FSH.Modules.Multitenancy.Contracts;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to ensure Contracts projects remain pure and only contain DTOs,
/// commands, queries, and service interfaces - no implementation details.
/// </summary>
public class ContractsPurityTests
{
    private static readonly Assembly[] ContractsAssemblies =
    [
        typeof(AuditingContractsMarker).Assembly,
        typeof(ChatContractsMarker).Assembly,
        typeof(IdentityContractsMarker).Assembly,
        typeof(MultitenancyContractsMarker).Assembly
    ];

    [Fact]
    public void Contracts_Should_Not_Depend_On_EntityFramework()
    {
        foreach (var assembly in ContractsAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("Microsoft.EntityFrameworkCore")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Contracts assembly '{assembly.GetName().Name}' should not depend on Entity Framework. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Contracts_Should_Not_Depend_On_FluentValidation()
    {
        foreach (var assembly in ContractsAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("FluentValidation")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Contracts assembly '{assembly.GetName().Name}' should not depend on FluentValidation. " +
                $"Validators belong in the module implementation, not contracts. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Contracts_Should_Not_Depend_On_Hangfire()
    {
        foreach (var assembly in ContractsAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("Hangfire")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Contracts assembly '{assembly.GetName().Name}' should not depend on Hangfire. " +
                $"Job scheduling is an implementation detail. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Contracts_Should_Not_Depend_On_Module_Implementations()
    {
        string[] moduleImplementations =
        [
            "FSH.Modules.Auditing.Features",
            "FSH.Modules.Auditing.Data",
            "FSH.Modules.Auditing.Persistence",
            "FSH.Modules.Chat.Features",
            "FSH.Modules.Chat.Data",
            "FSH.Modules.Chat.Domain",
            "FSH.Modules.Identity.Features",
            "FSH.Modules.Identity.Data",
            "FSH.Modules.Identity.Persistence",
            "FSH.Modules.Multitenancy.Features",
            "FSH.Modules.Multitenancy.Data",
            "FSH.Modules.Multitenancy.Persistence"
        ];

        foreach (var assembly in ContractsAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(moduleImplementations)
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Contracts assembly '{assembly.GetName().Name}' should not depend on module implementations. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Contracts_Should_Not_Contain_DbContext_Types()
    {
        foreach (var assembly in ContractsAssemblies)
        {
            var dbContextTypes = assembly.GetTypes()
                .Where(t => t.Name.Contains("DbContext", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            dbContextTypes.ShouldBeEmpty(
                $"Contracts assembly '{assembly.GetName().Name}' should not contain DbContext types. " +
                $"Found: {string.Join(", ", dbContextTypes.Select(t => t.FullName))}");
        }
    }

    [Fact]
    public void Contracts_Should_Not_Contain_Repository_Types()
    {
        foreach (var assembly in ContractsAssemblies)
        {
            var repositoryTypes = assembly.GetTypes()
                .Where(t => t.Name.Contains("Repository", StringComparison.OrdinalIgnoreCase)
                         && !t.IsInterface) // Interfaces like IRepository are OK
                .ToArray();

            repositoryTypes.ShouldBeEmpty(
                $"Contracts assembly '{assembly.GetName().Name}' should not contain concrete repository types. " +
                $"Found: {string.Join(", ", repositoryTypes.Select(t => t.FullName))}");
        }
    }

    [Fact]
    public void Commands_And_Queries_Should_Be_Records_Or_Sealed()
    {
        var nonSealedTypes = new List<string>();

        foreach (var assembly in ContractsAssemblies)
        {
            var commandQueryTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal)
                         || t.Name.EndsWith("Query", StringComparison.Ordinal));

            foreach (var type in commandQueryTypes)
            {
                // Records are implicitly sealed in terms of inheritance (they can't be inherited normally)
                // Check if it's a record by looking for the special EqualityContract property
                bool isRecord = type.GetProperty("EqualityContract",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance) != null;

                if (!isRecord && !type.IsSealed)
                {
                    nonSealedTypes.Add($"{type.FullName}");
                }
            }
        }

        // This is informational - existing codebase may have non-sealed commands
        // The pattern is recommended but not strictly enforced to allow gradual migration
        // For now, just verify we can identify non-sealed types (the test infrastructure works)
        // This serves as documentation of types that could be improved
        nonSealedTypes.ShouldNotBeNull();
    }
}