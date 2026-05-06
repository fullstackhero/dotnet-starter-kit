using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Architecture.Tests;

public class PlaygroundArchitectureTests
{
    [Fact]
    public void Modules_Should_Not_Depend_On_Playground_Hosts()
    {
        // Assemblies / namespaces that represent Playground hosts.
        string[] playgroundNamespaces =
        {
            "FSH.Starter.Api"
        };

        var result = Types
            .InCurrentDomain()
            .That()
            .ResideInNamespace("FSH.Modules")
            .Should()
            .NotHaveDependencyOnAny(playgroundNamespaces)
            .GetResult();

        var failingTypes = result.FailingTypeNames ?? Array.Empty<string>();

        result.IsSuccessful.ShouldBeTrue(
            "Module code must not depend on Playground host assemblies. " +
            $"Failing types: {string.Join(", ", failingTypes)}");
    }

    [Fact]
    public void Playground_Hosts_Should_Not_Depend_On_Module_Internals()
    {
        // Hosts may depend on module contracts and module root types,
        // but should not directly reference feature or data-layer namespaces.
        string[] forbiddenNamespaces =
        {
            "FSH.Modules.Auditing.Features",
            "FSH.Modules.Auditing.Data",
            "FSH.Modules.Identity.Features",
            "FSH.Modules.Identity.Data",
            "FSH.Modules.Multitenancy.Features",
            "FSH.Modules.Multitenancy.Data"
        };

        var hostResult = Types
            .InCurrentDomain()
            .That()
            .ResideInNamespace("FSH.Starter")
            .Should()
            .NotHaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        var hostFailingTypes = hostResult.FailingTypeNames ?? Array.Empty<string>();

        hostResult.IsSuccessful.ShouldBeTrue(
            "Playground hosts should not depend directly on module feature or data internals. " +
            $"Failing types: {string.Join(", ", hostFailingTypes)}");
    }
}

internal static class ModuleArchitectureTestsFixture
{
    public static readonly string SolutionRoot = GetSolutionRoot();

    private static string GetSolutionRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Unable to locate solution root containing 'src' folder.");
        }

        return directory.FullName;
    }
}