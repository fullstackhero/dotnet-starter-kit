using FSH.Framework.Core.Messaging.CQRS;
using FSH.Modules.Common.Core.Messaging.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Messaging.CQRS;
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        // dynamic dispatch to call HandleAsync(command, ct)
        return handler.HandleAsync((dynamic)command, ct);
    }
}