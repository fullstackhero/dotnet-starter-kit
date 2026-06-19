using FSH.Framework.Shared.Identity.Authorization;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Guards the permission-metadata contract between <c>RequiredPermissionAttribute</c> and
/// <c>RequiredPermissionAuthorizationHandler</c>. The handler resolves endpoint permissions via
/// <see cref="IRequiredPermissionMetadata"/>; if a duplicate <c>RequiredPermissionAttribute</c>
/// appears in another assembly without implementing that interface, endpoints decorated with it
/// carry no recognizable metadata and every <c>.RequirePermission()</c> gate silently fails open.
/// </summary>
public class AuthorizationMetadataTests
{
    private const string AttributeName = "RequiredPermissionAttribute";
    private const string ExpectedNamespace = "FSH.Framework.Shared.Identity.Authorization";

    [Fact]
    public void RequiredPermissionAttribute_Should_Exist_Exactly_Once_Across_All_FSH_Assemblies()
    {
        var matches = GetAllFshAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(t => string.Equals(t.Name, AttributeName, StringComparison.Ordinal))
            .ToArray();

        matches.ShouldNotBeEmpty(
            $"{AttributeName} was not found in any FSH assembly. " +
            "The permission authorization pipeline depends on it.");

        matches.Length.ShouldBe(1,
            $"Exactly one {AttributeName} must exist across all FSH assemblies. " +
            "A duplicate that does not implement IRequiredPermissionMetadata silently disables " +
            $"every .RequirePermission() gate. Found: {string.Join(", ", matches.Select(t => $"{t.FullName} ({t.Assembly.GetName().Name})"))}");

        matches[0].Namespace.ShouldBe(ExpectedNamespace,
            $"{AttributeName} must live in {ExpectedNamespace}, where " +
            "RequiredPermissionAuthorizationHandler resolves its metadata from.");
    }

    [Fact]
    public void RequiredPermissionAttribute_Should_Implement_IRequiredPermissionMetadata()
    {
        var attributeType = typeof(RequiredPermissionAttribute);

        typeof(IRequiredPermissionMetadata).IsAssignableFrom(attributeType).ShouldBeTrue(
            $"{attributeType.FullName} must implement IRequiredPermissionMetadata. " +
            "RequiredPermissionAuthorizationHandler discovers endpoint permissions through that " +
            "interface; without it, every .RequirePermission() gate silently fails open.");
    }

    /// <summary>
    /// Loads every FSH.* assembly deployed alongside the tests so the duplicate sweep covers
    /// BuildingBlocks, all modules (including Contracts), and host assemblies — not just the
    /// runtime module assemblies that ModuleAssemblyDiscovery returns.
    /// </summary>
    private static Assembly[] GetAllFshAssemblies()
    {
        string baseDir = AppContext.BaseDirectory;

        var assemblies = new List<Assembly>();

        foreach (var file in Directory.GetFiles(baseDir, "FSH.*.dll"))
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(file);
                assemblies.Add(Assembly.Load(assemblyName));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
            {
                // Skip if not a valid .NET assembly or other load error
            }
#pragma warning restore CA1031
        }

        assemblies.ShouldNotBeEmpty(
            "No FSH.* assemblies were found in the test output directory; the duplicate sweep would be a no-op.");

        return [.. assemblies];
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
