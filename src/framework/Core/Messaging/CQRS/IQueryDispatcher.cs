namespace FSH.Framework.Core.Messaging.CQRS;
public interface IQueryDispatcher
{
    /// <summary>
    /// Sends a query to its handler.
    /// </summary>
    Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResponse>;
}
