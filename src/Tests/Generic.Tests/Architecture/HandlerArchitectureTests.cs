using Mediator;
using System.Reflection;
using System.Text;

namespace Generic.Tests.Architecture;

/// <summary>
/// Architecture tests to ensure all handlers follow consistent patterns
/// across all modules (null checks, naming conventions, etc.).
/// </summary>
public sealed class HandlerArchitectureTests
{
    private static readonly Assembly[] ModuleAssemblies = ModuleAssemblyDiscovery.GetModuleAssemblies();

    [Fact]
    public void QueryHandlers_Should_FollowNamingConvention()
    {
        // Arrange
        var failures = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

            foreach (var handlerType in handlerTypes)
            {
                if (!handlerType.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
                {
                    failures.Add($"{handlerType.FullName} should end with 'QueryHandler'");
                }
            }
        }

        // Assert
        failures.ShouldBeEmpty(BuildFailureMessage(failures));
    }

    [Fact]
    public void CommandHandlers_Should_FollowNamingConvention()
    {
        // Arrange
        var failures = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))));

            foreach (var handlerType in handlerTypes)
            {
                if (!handlerType.Name.EndsWith("CommandHandler", StringComparison.Ordinal))
                {
                    failures.Add($"{handlerType.FullName} should end with 'CommandHandler'");
                }
            }
        }

        // Assert
        failures.ShouldBeEmpty(BuildFailureMessage(failures));
    }

    [Fact]
    public void Handlers_Should_BeSealed()
    {
        // Arrange
        var failures = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))));

            foreach (var handlerType in handlerTypes)
            {
                if (!handlerType.IsSealed)
                {
                    failures.Add($"{handlerType.FullName} should be sealed");
                }
            }
        }

        // Assert
        failures.ShouldBeEmpty(BuildFailureMessage(failures));
    }

    [Fact]
    public void Handlers_Should_HaveHandleMethod_WithCancellationToken()
    {
        // Arrange
        var failures = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))));

            foreach (var handlerType in handlerTypes)
            {
                var handleMethods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "Handle");

                foreach (var method in handleMethods)
                {
                    var parameters = method.GetParameters();
                    var hasCancellationToken = parameters.Any(p => p.ParameterType == typeof(CancellationToken));

                    if (!hasCancellationToken)
                    {
                        failures.Add($"{handlerType.FullName}.Handle() should have CancellationToken parameter");
                    }
                }
            }
        }

        // Assert
        failures.ShouldBeEmpty(BuildFailureMessage(failures));
    }

    [Fact]
    public void Validators_Should_FollowNamingConvention()
    {
        // Arrange
        var failures = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            var validatorTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.BaseType != null &&
                           t.BaseType.IsGenericType &&
                           t.BaseType.GetGenericTypeDefinition().Name.Contains("AbstractValidator", StringComparison.Ordinal));

            foreach (var validatorType in validatorTypes)
            {
                if (!validatorType.Name.EndsWith("Validator", StringComparison.Ordinal))
                {
                    failures.Add($"{validatorType.FullName} should end with 'Validator'");
                }
            }
        }

        // Assert
        failures.ShouldBeEmpty(BuildFailureMessage(failures));
    }

    [Fact]
    public void Validators_Should_BeSealed()
    {
        // Arrange
        var failures = new List<string>();

        foreach (var assembly in ModuleAssemblies)
        {
            var validatorTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.BaseType != null &&
                           t.BaseType.IsGenericType &&
                           t.BaseType.GetGenericTypeDefinition().Name.Contains("AbstractValidator", StringComparison.Ordinal));

            foreach (var validatorType in validatorTypes)
            {
                // Skip partial classes (e.g., UpdateTenantThemeCommandValidator uses partial for source-generated regex)
                // Partial classes cannot be sealed, but their nested validators are sealed
                if (IsPartialClass(validatorType))
                {
                    continue;
                }

                if (!validatorType.IsSealed)
                {
                    failures.Add($"{validatorType.FullName} should be sealed");
                }
            }
        }

        // Assert
        failures.ShouldBeEmpty(BuildFailureMessage(failures));
    }

    private static bool IsPartialClass(Type type)
    {
        // Source-generator partials (e.g. GeneratedRegex) emit compiler-generated members; detect
        // them via a GeneratedRegexAttribute on any method.
        return type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Any(m => m.GetCustomAttributes()
                .Any(a => a.GetType().Name == "GeneratedRegexAttribute"));
    }

    private static string BuildFailureMessage(List<string> failures)
    {
        if (failures.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("Found ").Append(failures.Count).AppendLine(" violation(s):");
        foreach (var failure in failures)
        {
            sb.Append("  - ").AppendLine(failure);
        }
        return sb.ToString();
    }
}