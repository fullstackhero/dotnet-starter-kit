using FSH.Modules.Common.Core.Messaging.CQRS;

namespace Architecture.Tests.Messaging;

public class CommandHandlerNamingTests
{
    [Fact]
    public void All_ICommandHandler_Implementations_Should_End_With_CommandHandler()
    {
        var handlerInterfaceType = typeof(ICommandHandler<,>);

        var assemblies = ModuleAssemblyLoader.GetFshAssemblies();

        var failures = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == handlerInterfaceType))
            .Where(t => !t.Name.EndsWith("CommandHandler", StringComparison.Ordinal))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(failures.Count == 0,
            $"The following classes do not end with 'CommandHandler': {string.Join(", ", failures)}");
    }
}