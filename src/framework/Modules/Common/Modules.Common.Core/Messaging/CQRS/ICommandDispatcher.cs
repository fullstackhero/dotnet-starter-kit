using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Core.Messaging.CQRS;
public interface ICommandDispatcher
{
    /// <summary>
    /// Sends a command to its handler.
    /// </summary>
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
}