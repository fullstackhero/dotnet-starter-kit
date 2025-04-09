using FSH.Framework.Core.Messaging.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Messaging.CQRS;
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return handler.HandleAsync(command, ct);
    }
}
