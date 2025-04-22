using FSH.Framework.Core.Messaging.CQRS;

namespace Architecture.Tests.Messaging;
public class QueryHandlerNamingTests
{
    [Fact]
    public void All_IQueryHandler_Implementations_Should_End_With_QueryHandler()
    {
        var handlerInterfaceType = typeof(IQueryHandler<,>);

        var assemblies = ModuleAssemblyLoader.GetFshAssemblies();

        var failures = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == handlerInterfaceType))
            .Where(t => !t.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(failures.Count == 0,
            $"The following classes do not end with 'QueryHandler': {string.Join(", ", failures)}");
    }
}