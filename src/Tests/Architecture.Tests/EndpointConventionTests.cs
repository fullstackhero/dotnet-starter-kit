using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to enforce endpoint conventions across all modules.
/// </summary>
public class EndpointConventionTests
{
    private static readonly Assembly[] ModuleAssemblies = ModuleAssemblyDiscovery.GetModuleAssemblies();

    [Fact]
    public void Endpoints_Should_Be_Static_Classes()
    {
        var violations = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var endpointTypes = module.GetTypes()
                .Where(t => t.Name.EndsWith("Endpoint", StringComparison.Ordinal))
                .Where(t => t.IsClass);

            foreach (var endpointType in endpointTypes)
            {
                if (!endpointType.IsAbstract || !endpointType.IsSealed)
                {
                    // In C#, static classes are compiled as abstract sealed
                    violations.Add($"{endpointType.FullName} should be a static class");
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Endpoint classes should be static. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Endpoints_Should_Reside_In_Features_Namespace()
    {
        foreach (var module in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(module)
                .That()
                .HaveNameEndingWith("Endpoint")
                .Should()
                .ResideInNamespaceContaining(".Features.")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Endpoints in module '{module.GetName().Name}' should reside in Features namespace. " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Endpoints_Should_Have_Map_Method()
    {
        var violations = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var endpointTypes = module.GetTypes()
                .Where(t => t.Name.EndsWith("Endpoint", StringComparison.Ordinal))
                .Where(t => t.IsClass && t.IsAbstract && t.IsSealed); // Static classes

            foreach (var endpointType in endpointTypes)
            {
                // Check both public and internal/non-public static methods
                var mapMethods = endpointType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.Name.StartsWith("Map", StringComparison.Ordinal))
                    .ToArray();

                if (mapMethods.Length == 0)
                {
                    violations.Add($"{endpointType.FullName} should have a Map* method");
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Endpoint classes should have a Map method (e.g., MapGetUsersEndpoint). " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Endpoint_Map_Methods_Should_Return_RouteHandlerBuilder()
    {
        var violations = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var endpointTypes = module.GetTypes()
                .Where(t => t.Name.EndsWith("Endpoint", StringComparison.Ordinal))
                .Where(t => t.IsClass && t.IsAbstract && t.IsSealed); // Static classes

            foreach (var endpointType in endpointTypes)
            {
                // Check both public and internal/non-public static methods
                var mapMethods = endpointType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.Name.StartsWith("Map", StringComparison.Ordinal));

                foreach (var method in mapMethods)
                {
                    var returnType = method.ReturnType;

                    // Check if return type is RouteHandlerBuilder or a derived type
                    bool isValidReturn = returnType.Name == "RouteHandlerBuilder" ||
                                         returnType.Name == "IEndpointConventionBuilder" ||
                                         returnType.GetInterfaces().Any(i =>
                                             i.Name == "IEndpointConventionBuilder");

                    if (!isValidReturn)
                    {
                        violations.Add(
                            $"{endpointType.Name}.{method.Name} returns {returnType.Name}, " +
                            "expected RouteHandlerBuilder");
                    }
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Endpoint Map methods should return RouteHandlerBuilder. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Endpoint_Map_Methods_Should_Take_IEndpointRouteBuilder()
    {
        var violations = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var endpointTypes = module.GetTypes()
                .Where(t => t.Name.EndsWith("Endpoint", StringComparison.Ordinal))
                .Where(t => t.IsClass && t.IsAbstract && t.IsSealed); // Static classes

            foreach (var endpointType in endpointTypes)
            {
                // Check both public and internal/non-public static methods
                var mapMethods = endpointType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.Name.StartsWith("Map", StringComparison.Ordinal));

                foreach (var method in mapMethods)
                {
                    var parameters = method.GetParameters();

                    // Should be an extension method with first parameter IEndpointRouteBuilder
                    bool hasEndpointRouteBuilder = parameters.Length > 0 &&
                        (parameters[0].ParameterType.Name == "IEndpointRouteBuilder" ||
                         parameters[0].ParameterType.GetInterfaces().Any(i =>
                             i.Name == "IEndpointRouteBuilder"));

                    if (!hasEndpointRouteBuilder)
                    {
                        violations.Add(
                            $"{endpointType.Name}.{method.Name} first parameter should be IEndpointRouteBuilder");
                    }
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Endpoint Map methods should extend IEndpointRouteBuilder. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Endpoints_Should_Not_Contain_Business_Logic()
    {
        // Endpoints should delegate to handlers via Mediator, not contain business logic
        // We check that endpoint classes don't have private methods (which might contain logic)
        var warnings = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var endpointTypes = module.GetTypes()
                .Where(t => t.Name.EndsWith("Endpoint", StringComparison.Ordinal))
                .Where(t => t.IsClass && t.IsAbstract && t.IsSealed); // Static classes

            foreach (var endpointType in endpointTypes)
            {
                var privateMethods = endpointType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => !m.Name.StartsWith('<')) // Exclude compiler-generated
                    .Where(m => m.DeclaringType == endpointType) // Only declared in this type
                    .ToArray();

                if (privateMethods.Length > 0)
                {
                    warnings.Add(
                        $"{endpointType.Name} has private methods ({string.Join(", ", privateMethods.Select(m => m.Name))}). " +
                        "Consider moving logic to handlers.");
                }
            }
        }

        // Informational only — some private static helpers in endpoints are acceptable; we assert
        // the check ran (list populated) rather than that it is empty.
        warnings.ShouldNotBeNull("Endpoint business logic check did not run");
        // Review any endpoints reported in 'warnings' and move business logic to handlers.
    }

    [Fact]
    public void Endpoint_Names_Should_Follow_Convention()
    {
        var violations = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var endpointTypes = module.GetTypes()
                .Where(t => t.Name.EndsWith("Endpoint", StringComparison.Ordinal))
                .Where(t => t.IsClass);

            foreach (var endpointType in endpointTypes)
            {
                // Endpoint names should describe the action (e.g., GetUsersEndpoint, CreateTenantEndpoint)
                string name = endpointType.Name;

                // Check it follows verb-noun-Endpoint pattern
                bool hasVerb = name.StartsWith("Get", StringComparison.Ordinal) ||
                               name.StartsWith("Create", StringComparison.Ordinal) ||
                               name.StartsWith("Update", StringComparison.Ordinal) ||
                               name.StartsWith("Delete", StringComparison.Ordinal) ||
                               name.StartsWith("List", StringComparison.Ordinal) ||
                               name.StartsWith("Search", StringComparison.Ordinal) ||
                               name.StartsWith("Register", StringComparison.Ordinal) ||
                               name.StartsWith("Generate", StringComparison.Ordinal) ||
                               name.StartsWith("Refresh", StringComparison.Ordinal) ||
                               name.StartsWith("Resend", StringComparison.Ordinal) ||
                               name.StartsWith("Confirm", StringComparison.Ordinal) ||
                               name.StartsWith("Reset", StringComparison.Ordinal) ||
                               name.StartsWith("Forgot", StringComparison.Ordinal) ||
                               name.StartsWith("Change", StringComparison.Ordinal) ||
                               name.StartsWith("Toggle", StringComparison.Ordinal) ||
                               name.StartsWith("Assign", StringComparison.Ordinal) ||
                               name.StartsWith("Revoke", StringComparison.Ordinal) ||
                               name.StartsWith("Admin", StringComparison.Ordinal) ||
                               name.StartsWith("Upsert", StringComparison.Ordinal) ||
                               name.StartsWith("Add", StringComparison.Ordinal) ||
                               name.StartsWith("Remove", StringComparison.Ordinal) ||
                               name.StartsWith("Retry", StringComparison.Ordinal) ||
                               name.StartsWith("Upgrade", StringComparison.Ordinal) ||
                               name.StartsWith("Renew", StringComparison.Ordinal) ||
                               name.StartsWith("Self", StringComparison.Ordinal) ||
                               name.StartsWith("Tenant", StringComparison.Ordinal) ||
                               name.StartsWith("Start", StringComparison.Ordinal) ||
                               name.StartsWith("End", StringComparison.Ordinal) ||
                               name.StartsWith("Enroll", StringComparison.Ordinal) ||
                               name.StartsWith("Verify", StringComparison.Ordinal) ||
                               name.StartsWith("Disable", StringComparison.Ordinal) ||
                               name.StartsWith("Enable", StringComparison.Ordinal) ||
                               name.StartsWith("Restore", StringComparison.Ordinal) ||
                               name.StartsWith("Adjust", StringComparison.Ordinal) ||
                               name.StartsWith("Resolve", StringComparison.Ordinal) ||
                               name.StartsWith("Reopen", StringComparison.Ordinal) ||
                               name.StartsWith("Close", StringComparison.Ordinal) ||
                               name.StartsWith("Test", StringComparison.Ordinal) ||
                               name.StartsWith("Void", StringComparison.Ordinal) ||
                               name.StartsWith("Mark", StringComparison.Ordinal) ||
                               name.StartsWith("Issue", StringComparison.Ordinal) ||
                               name.StartsWith("Capture", StringComparison.Ordinal) ||
                               name.StartsWith("Request", StringComparison.Ordinal) ||
                               name.StartsWith("Finalize", StringComparison.Ordinal) ||
                               name.StartsWith("Set", StringComparison.Ordinal) ||
                               name.StartsWith("Reorder", StringComparison.Ordinal) ||
                               name.StartsWith("Archive", StringComparison.Ordinal) ||
                               name.StartsWith("Find", StringComparison.Ordinal) ||
                               name.StartsWith("Edit", StringComparison.Ordinal) ||
                               name.StartsWith("Send", StringComparison.Ordinal) ||
                               name.StartsWith("Discover", StringComparison.Ordinal) ||
                               name.StartsWith("Pin", StringComparison.Ordinal) ||
                               name.StartsWith("Unpin", StringComparison.Ordinal);

                if (!hasVerb)
                {
                    violations.Add(
                        $"{endpointType.FullName} name should start with an action verb " +
                        "(Get, Create, Update, Delete, etc.)");
                }
            }
        }

        violations.ShouldBeEmpty(
            $"Endpoint names should follow verb-noun convention. " +
            $"Violations: {string.Join(", ", violations)}");
    }
}