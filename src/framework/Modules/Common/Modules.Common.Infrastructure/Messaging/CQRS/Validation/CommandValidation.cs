using FSH.Framework.Core.Messaging.CQRS;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Infrastructure.Messaging.CQRS.Validation;
public class CommandValidation : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;
    private readonly IServiceProvider _serviceProvider;

    public CommandValidation(ICommandDispatcher inner, IServiceProvider serviceProvider)
    {
        _inner = inner;
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(command, _serviceProvider, ct);
        return await _inner.SendAsync(command, ct);
    }
}