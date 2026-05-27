using Shouldly;
using System.Xml.Linq;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to detect circular project references in the solution.
/// Circular references cause build issues and indicate architectural problems.
/// </summary>
public class CircularReferenceTests
{
    private static readonly string SolutionRoot = ModuleArchitectureTestsFixture.SolutionRoot;

    [Fact]
    public void Solution_Should_Not_Have_Circular_Project_References()
    {
        string srcRoot = Path.Combine(SolutionRoot, "src");

        // Build the dependency graph
        var projectPaths = Directory
            .GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var dependencyGraph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectPath in projectPaths)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            var dependencies = GetProjectReferences(projectPath);
            dependencyGraph[projectName] = dependencies;
        }

        // Detect cycles using DFS
        var cycles = DetectCycles(dependencyGraph);

        cycles.ShouldBeEmpty(
            $"Circular project references detected: {string.Join("; ", cycles)}");
    }

    [Fact]
    public void Modules_Should_Not_Have_Circular_Dependencies()
    {
        string modulesRoot = Path.Combine(SolutionRoot, "src", "Modules");

        if (!Directory.Exists(modulesRoot))
        {
            return;
        }

        var moduleProjects = Directory
            .GetFiles(modulesRoot, "Modules.*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var dependencyGraph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectPath in moduleProjects)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            var dependencies = GetProjectReferences(projectPath)
                .Where(d => d.StartsWith("Modules.", StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            dependencyGraph[projectName] = dependencies;
        }

        var cycles = DetectCycles(dependencyGraph);

        cycles.ShouldBeEmpty(
            $"Circular module dependencies detected: {string.Join("; ", cycles)}");
    }

    [Fact]
    public void BuildingBlocks_Should_Not_Have_Circular_Dependencies()
    {
        string buildingBlocksRoot = Path.Combine(SolutionRoot, "src", "BuildingBlocks");

        if (!Directory.Exists(buildingBlocksRoot))
        {
            return;
        }

        var buildingBlockProjects = Directory
            .GetFiles(buildingBlocksRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var dependencyGraph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectPath in buildingBlockProjects)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            var dependencies = GetProjectReferences(projectPath);
            dependencyGraph[projectName] = dependencies;
        }

        var cycles = DetectCycles(dependencyGraph);

        cycles.ShouldBeEmpty(
            $"Circular BuildingBlock dependencies detected: {string.Join("; ", cycles)}");
    }

    [Fact]
    public void Dependency_Graph_Should_Be_Acyclic()
    {
        string srcRoot = Path.Combine(SolutionRoot, "src");

        var projectPaths = Directory
            .GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var dependencyGraph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectPath in projectPaths)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            var dependencies = GetProjectReferences(projectPath);
            dependencyGraph[projectName] = dependencies;
        }

        // Attempt topological sort - will fail if cycles exist
        _ = TopologicalSort(dependencyGraph, out var hasCycle, out var cycleDescription);

        hasCycle.ShouldBeFalse(
            $"Dependency graph is not acyclic. {cycleDescription}");
    }

    private static HashSet<string> GetProjectReferences(string projectPath)
    {
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var document = XDocument.Load(projectPath);

            var projectRefs = document
                .Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .Where(include => include.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name));

            foreach (var reference in projectRefs)
            {
                references.Add(reference!);
            }
        }
        catch (System.Xml.XmlException)
        {
            // Ignore XML parse errors
        }
        catch (IOException)
        {
            // Ignore file IO errors
        }

        return references;
    }

    private static List<string> DetectCycles(Dictionary<string, HashSet<string>> graph)
    {
        var cycles = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        foreach (var node in graph.Keys)
        {
            if (DetectCyclesDfs(node, graph, visited, recursionStack, path, cycles))
            {
                // Found at least one cycle
            }
        }

        return cycles;
    }

    private static bool DetectCyclesDfs(
        string node,
        Dictionary<string, HashSet<string>> graph,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path,
        List<string> cycles)
    {
        if (recursionStack.Contains(node))
        {
            // Found a cycle - extract the cycle path
            int cycleStart = path.IndexOf(node);
            if (cycleStart >= 0)
            {
                var cyclePath = path.Skip(cycleStart).Append(node).ToArray();
                cycles.Add(string.Join(" -> ", cyclePath));
            }
            return true;
        }

        if (visited.Contains(node))
        {
            return false;
        }

        visited.Add(node);
        recursionStack.Add(node);
        path.Add(node);

        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                DetectCyclesDfs(neighbor, graph, visited, recursionStack, path, cycles);
            }
        }

        path.RemoveAt(path.Count - 1);
        recursionStack.Remove(node);

        return false;
    }

    private static List<string> TopologicalSort(
        Dictionary<string, HashSet<string>> graph,
        out bool hasCycle,
        out string cycleDescription)
    {
        hasCycle = false;
        cycleDescription = string.Empty;

        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var temporaryMark = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node) &&
                !TopologicalSortVisit(node, graph, visited, temporaryMark, result, out cycleDescription))
            {
                hasCycle = true;
                return result;
            }
        }

        result.Reverse();
        return result;
    }

    private static bool TopologicalSortVisit(
        string node,
        Dictionary<string, HashSet<string>> graph,
        HashSet<string> visited,
        HashSet<string> temporaryMark,
        List<string> result,
        out string cycleDescription)
    {
        cycleDescription = string.Empty;

        if (temporaryMark.Contains(node))
        {
            cycleDescription = $"Cycle detected at node: {node}";
            return false;
        }

        if (visited.Contains(node))
        {
            return true;
        }

        temporaryMark.Add(node);

        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!TopologicalSortVisit(neighbor, graph, visited, temporaryMark, result, out cycleDescription))
                {
                    cycleDescription = $"{node} -> {cycleDescription}";
                    return false;
                }
            }
        }

        temporaryMark.Remove(node);
        visited.Add(node);
        result.Add(node);

        return true;
    }
}