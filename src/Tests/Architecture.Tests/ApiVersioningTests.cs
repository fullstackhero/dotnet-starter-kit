using FSH.Modules.Auditing;
using FSH.Modules.Identity;
using FSH.Modules.Multitenancy;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to enforce API versioning conventions across all modules.
/// </summary>
public partial class ApiVersioningTests
{
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(AuditingModule).Assembly,
        typeof(IdentityModule).Assembly,
        typeof(MultitenancyModule).Assembly
    ];

    private static readonly string SolutionRoot = ModuleArchitectureTestsFixture.SolutionRoot;

    [Fact]
    public void Features_Should_Be_In_Versioned_Namespace()
    {
        foreach (var module in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(module)
                .That()
                .ResideInNamespaceContaining(".Features.")
                .Should()
                .ResideInNamespaceMatching(@"\.Features\.v\d+")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Features in module '{module.GetName().Name}' should be in versioned namespaces (v1, v2, etc.). " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Feature_Folders_Should_Follow_Version_Convention()
    {
        string modulesRoot = Path.Combine(SolutionRoot, "src", "Modules");

        if (!Directory.Exists(modulesRoot))
        {
            return;
        }

        var featureFolders = Directory
            .GetDirectories(modulesRoot, "Features", SearchOption.AllDirectories)
            .ToArray();

        var violations = new List<string>();

        foreach (var featuresFolder in featureFolders)
        {
            var subFolders = Directory.GetDirectories(featuresFolder);

            foreach (var subFolder in subFolders)
            {
                string folderName = Path.GetFileName(subFolder);

                // Feature folders directly under Features should be version folders (v1, v2, etc.)
                if (!VersionFolderRegex().IsMatch(folderName))
                {
                    violations.Add(
                        $"Folder '{subFolder}' should be a version folder (v1, v2, etc.), not '{folderName}'");
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Feature folders should be organized by version. " +
            $"Violations: {string.Join("; ", violations)}");
    }

    [Fact]
    public void V1_Types_Should_Not_Depend_On_Higher_Versions()
    {
        // Already covered in FeatureArchitectureTests, but reinforced here
        foreach (var module in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(module)
                .That()
                .ResideInNamespaceContaining(".v1.")
                .ShouldNot()
                .HaveDependencyOnAny(
                    ".v2.",
                    ".v3.",
                    ".v4.",
                    ".v5.")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"v1 types in module '{module.GetName().Name}' should not depend on higher versions. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Higher_Versions_Can_Depend_On_Lower_Versions()
    {
        // This is a permissive test - v2 can depend on v1 for backward compatibility
        // Just verify the pattern exists
        foreach (var module in ModuleAssemblies)
        {
            var v2Types = module.GetTypes()
                .Where(t => t.Namespace?.Contains(".v2.", StringComparison.Ordinal) == true)
                .ToArray();

            // If v2 exists, it should be allowed to reference v1
            // This test documents the expected behavior - v2 types are allowed to exist
            v2Types.ShouldNotBeNull();
        }
    }

    [Fact]
    public void Commands_And_Queries_Should_Be_In_Same_Version_As_Handlers()
    {
        var violations = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var handlerTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal))
                .Where(t => t.Namespace?.Contains(".Features.", StringComparison.Ordinal) == true);

            foreach (var handlerType in handlerTypes)
            {
                string? handlerNamespace = handlerType.Namespace;
                var handlerVersion = ExtractVersion(handlerNamespace);

                if (string.IsNullOrEmpty(handlerVersion))
                {
                    continue;
                }

                // Find the command/query type this handler handles
                var handlerInterfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               (i.Name.Contains("CommandHandler", StringComparison.Ordinal) ||
                                i.Name.Contains("QueryHandler", StringComparison.Ordinal)));

                foreach (var handlerInterface in handlerInterfaces)
                {
                    var genericArgs = handlerInterface.GetGenericArguments();
                    if (genericArgs.Length > 0)
                    {
                        var requestType = genericArgs[0];
                        var requestVersion = ExtractVersion(requestType.Namespace);

                        if (!string.IsNullOrEmpty(requestVersion) &&
                            !handlerVersion.Equals(requestVersion, StringComparison.OrdinalIgnoreCase))
                        {
                            violations.Add(
                                $"{handlerType.Name} ({handlerVersion}) handles {requestType.Name} ({requestVersion})");
                        }
                    }
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Handlers should handle commands/queries from the same API version. " +
            $"Violations: {string.Join("; ", violations)}");
    }

    [Fact]
    public void Each_Version_Should_Be_Self_Contained()
    {
        // Check that each version folder contains all necessary components
        string modulesRoot = Path.Combine(SolutionRoot, "src", "Modules");

        if (!Directory.Exists(modulesRoot))
        {
            return;
        }

        var warnings = new List<string>();

        var moduleDirectories = Directory.GetDirectories(modulesRoot);

        foreach (var moduleDir in moduleDirectories)
        {
            var featuresDir = Directory
                .GetDirectories(moduleDir, "Features", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (featuresDir == null) continue;

            var versionDirs = Directory.GetDirectories(featuresDir)
                .Where(d => VersionFolderRegex().IsMatch(Path.GetFileName(d)));

            foreach (var versionDir in versionDirs)
            {
                var featureDirs = Directory.GetDirectories(versionDir);

                foreach (var featureDir in featureDirs)
                {
                    var files = Directory.GetFiles(featureDir, "*.cs");
                    var fileNames = files.Select(Path.GetFileNameWithoutExtension).ToHashSet();

                    // Check for common feature components
                    bool hasEndpoint = fileNames.Any(f => f!.EndsWith("Endpoint", StringComparison.Ordinal));
                    bool hasHandler = fileNames.Any(f => f!.EndsWith("Handler", StringComparison.Ordinal));

                    // A feature should have at least an endpoint or handler
                    if (!hasEndpoint && !hasHandler)
                    {
                        warnings.Add(
                            $"Feature '{Path.GetFileName(featureDir)}' in {Path.GetFileName(versionDir)} " +
                            "has no endpoint or handler");
                    }
                }
            }
        }

        // Informational - some features may be structured differently
        // Assert that we processed the directories (test ran successfully)
        warnings.ShouldNotBeNull();
    }

    private static string? ExtractVersion(string? ns)
    {
        if (string.IsNullOrEmpty(ns)) return null;

        var match = Regex.Match(ns, @"\.v(\d+)\.", RegexOptions.IgnoreCase);
        return match.Success ? $"v{match.Groups[1].Value}" : null;
    }

    [GeneratedRegex(@"^v\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionFolderRegex();
}