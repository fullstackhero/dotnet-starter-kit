using FSH.Modules.Auditing;
using FSH.Modules.Identity;
using FSH.Modules.Multitenancy;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Architecture.Tests;

public class FeatureArchitectureTests
{
    [Fact]
    public void Features_Versions_Should_Not_Depend_On_Newer_Versions()
    {
        // Guardrail for future versions (v2, v3, ...). For now, this is mostly
        // a safety net to prevent accidental cross-version feature coupling.
        var modules = new[]
        {
            typeof(AuditingModule).Assembly,
            typeof(IdentityModule).Assembly,
            typeof(MultitenancyModule).Assembly
        };

        foreach (var module in modules)
        {
            var v1Result = Types
                .InAssembly(module)
                .That()
                .ResideInNamespaceEndingWith(".Features.v1")
                .Should()
                .NotHaveDependencyOnAny(
                    // If v2+ namespaces are introduced later, v1 should not depend on them.
                    ".Features.v2",
                    ".Features.v3")
                .GetResult();

            var failingTypes = v1Result.FailingTypeNames ?? Array.Empty<string>();

            v1Result.IsSuccessful.ShouldBeTrue(
                $"v1 features in assembly '{module.FullName}' must not depend on newer feature versions. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }
}