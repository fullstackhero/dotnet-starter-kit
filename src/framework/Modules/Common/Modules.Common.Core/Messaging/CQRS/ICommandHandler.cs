namespace FSH.Modules.Common.Core.Messaging.CQRS;

/// <summary>
/// Handles a command and returns a result.
/// </summary>
/// <typeparam name="TCommand">Type of command</typeparam>
/// <typeparam name="TResponse">Type of response</typeparam>
public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}