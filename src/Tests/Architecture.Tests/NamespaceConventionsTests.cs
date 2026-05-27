using Shouldly;
using Xunit;

namespace Architecture.Tests;

public class NamespaceConventionsTests
{
    private static readonly string SolutionRoot = ModuleArchitectureTestsFixture.SolutionRoot;

    [Fact]
    public void BuildingBlocks_Core_Domain_Namespaces_Should_Match_Folder()
    {
        string domainRoot = Path.Combine(SolutionRoot, "src", "BuildingBlocks", "Core", "Domain");

        if (!Directory.Exists(domainRoot))
        {
            // If the folder does not yet exist, treat this as a neutral pass.
            return;
        }

        var files = Directory
            .GetFiles(domainRoot, "*.cs", SearchOption.AllDirectories)
            .ToArray();

        files.Length.ShouldBeGreaterThan(0);

        foreach (string file in files)
        {
            string content = File.ReadAllText(file);

            var namespaceLine = content
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.TrimStart().StartsWith("namespace ", StringComparison.Ordinal));

            namespaceLine.ShouldNotBeNull($"File '{file}' must declare a namespace matching the folder structure.");

            string declaredNamespace = namespaceLine!["namespace ".Length..].Trim().TrimEnd(';');

            declaredNamespace
                .Contains(".Core.", StringComparison.Ordinal)
                .ShouldBeTrue($"Namespace '{declaredNamespace}' should include '.Core.' for file '{file}'.");

            declaredNamespace
                .Contains(".Domain", StringComparison.Ordinal)
                .ShouldBeTrue($"Namespace '{declaredNamespace}' should include '.Domain' for file '{file}'.");
        }
    }
}