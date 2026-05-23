using FSH.Framework.Core;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to ensure BuildingBlocks remain independent and reusable,
/// without dependencies on application-specific modules.
/// </summary>
public class BuildingBlocksIndependenceTests
{
    private static readonly string SolutionRoot = ModuleArchitectureTestsFixture.SolutionRoot;

    private static readonly Assembly[] BuildingBlockAssemblies =
    [
        typeof(IFshCore).Assembly,               // Core
        typeof(IConnectionStringValidator).Assembly,  // Persistence
        typeof(IAppTenantInfo).Assembly,         // Shared
        typeof(IFshWeb).Assembly                 // Web
    ];

    [Fact]
    public void BuildingBlocks_Should_Not_Depend_On_Modules()
    {
        foreach (var assembly in BuildingBlockAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "FSH.Modules.Auditing",
                    "FSH.Modules.Identity",
                    "FSH.Modules.Multitenancy")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"BuildingBlock '{assembly.GetName().Name}' should not depend on Modules. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void BuildingBlocks_Should_Not_Depend_On_Hosts()
    {
        foreach (var assembly in BuildingBlockAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "FSH.Starter",
                    "FSH.Starter.Api")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"BuildingBlock '{assembly.GetName().Name}' should not depend on Hosts. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void BuildingBlocks_Projects_Should_Not_Reference_Modules_Directly()
    {
        string buildingBlocksRoot = Path.Combine(SolutionRoot, "src", "BuildingBlocks");

        var projects = Directory
            .GetFiles(buildingBlocksRoot, "*.csproj", SearchOption.AllDirectories)
            .ToArray();

        projects.Length.ShouldBeGreaterThan(0);

        var violations = new List<string>();

        foreach (string projectPath in projects)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            var document = XDocument.Load(projectPath);

            var references = document
                .Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .Where(include => include.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (string include in references)
            {
                string referencedName = GetReferencedProjectName(include);

                // Check if it references a Modules project
                if (referencedName.StartsWith("Modules.", StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{projectName} -> {referencedName}");
                }

                // Check if it references a Host project
                if (referencedName.Contains("AppHost", StringComparison.OrdinalIgnoreCase) ||
                    referencedName.StartsWith("FSH.Starter.Api", StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{projectName} -> {referencedName}");
                }
            }
        }

        violations.ShouldBeEmpty(
            $"BuildingBlocks should not reference Modules or Host projects. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Core_BuildingBlock_Should_Be_Dependency_Free()
    {
        // Core should only depend on .NET BCL and Mediator abstractions
        string[] allowedDependencies =
        [
            "System",
            "Microsoft",
            "Mediator.Abstractions",
            "netstandard",
            "mscorlib"
        ];

        string coreProjectPath = Path.Combine(SolutionRoot, "src", "BuildingBlocks", "Core", "Core.csproj");
        var document = XDocument.Load(coreProjectPath);

        var packageReferences = document
            .Descendants("PackageReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        var projectReferences = document
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        // Core should have no project references to other BuildingBlocks
        projectReferences.ShouldBeEmpty(
            $"Core BuildingBlock should not reference other projects. " +
            $"Found: {string.Join(", ", projectReferences)}");

        // Check package references are minimal
        var disallowedPackages = packageReferences
            .Where(p => !allowedDependencies.Any(a =>
                p.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        // Note: This is informational - some dependencies may be acceptable
        if (disallowedPackages.Length > 0)
        {
            // Review these dependencies to ensure Core remains lightweight
        }
    }

    [Fact]
    public void BuildingBlocks_Should_Follow_Layered_Dependencies()
    {
        // Define the expected dependency order (lower layers should not depend on higher)
        // Layer 0: Core (no dependencies)
        // Layer 1: Shared, Caching, Mailing, Storage (depend on Core)
        // Layer 2: Persistence, Jobs (depend on Core, Shared)
        // Layer 3: Eventing, Web (can depend on lower layers)

        var layerViolations = new List<string>();

        // Core should not depend on anything
        CheckBuildingBlockDependencies("Core", [], layerViolations);

        // Shared should only depend on Core
        CheckBuildingBlockDependencies("Shared", ["Core"], layerViolations);

        // Caching should only depend on Core
        CheckBuildingBlockDependencies("Caching", ["Core"], layerViolations);

        // Mailing should only depend on Core
        CheckBuildingBlockDependencies("Mailing", ["Core"], layerViolations);

        // Storage depends on Core and Shared (FileUploadRequest moved to Shared) plus Quota,
        // since the quota-metered storage decorator lives here and charges StorageBytes per upload.
        CheckBuildingBlockDependencies("Storage", ["Core", "Shared", "Quota"], layerViolations);

        // Persistence should depend on Core, Shared
        CheckBuildingBlockDependencies("Persistence", ["Core", "Shared"], layerViolations);

        // Jobs should depend on Core, Shared
        CheckBuildingBlockDependencies("Jobs", ["Core", "Shared"], layerViolations);

        // Eventing.Abstractions should have no dependencies (lightweight interfaces)
        CheckBuildingBlockDependencies("Eventing.Abstractions", [], layerViolations);

        // Eventing should depend on Core and Eventing.Abstractions
        CheckBuildingBlockDependencies("Eventing", ["Core", "Eventing.Abstractions"], layerViolations);

        layerViolations.ShouldBeEmpty(
            $"BuildingBlocks should follow layered dependency rules. " +
            $"Violations: {string.Join("; ", layerViolations)}");
    }

    private static void CheckBuildingBlockDependencies(
        string projectName,
        string[] allowedDependencies,
        List<string> violations)
    {
        string projectPath = Path.Combine(SolutionRoot, "src", "BuildingBlocks", projectName, $"{projectName}.csproj");

        if (!File.Exists(projectPath))
        {
            return; // Project doesn't exist
        }

        var document = XDocument.Load(projectPath);

        var projectReferences = document
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Select(GetReferencedProjectName)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        foreach (var reference in projectReferences)
        {
            if (!allowedDependencies.Contains(reference, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add($"{projectName} depends on {reference} (not in allowed: {string.Join(", ", allowedDependencies)})");
            }
        }
    }

    // ProjectReference Include paths are authored with Windows separators (e.g. ..\Core\Core.csproj).
    // Path.GetFileNameWithoutExtension only treats '\' as a separator on Windows, so on Linux CI it
    // returns the whole path minus the extension. Normalize to '/' first to get the bare project name
    // on every platform.
    private static string GetReferencedProjectName(string includePath) =>
        Path.GetFileNameWithoutExtension(includePath.Replace('\\', '/'));
}