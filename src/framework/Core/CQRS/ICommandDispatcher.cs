namespace FSH.Framework.Core.CQRS;
public interface ICommandDispatcher
{
    /// <summary>
    /// Sends a command to its handler.
    /// </summary>
    Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResponse>;
}
