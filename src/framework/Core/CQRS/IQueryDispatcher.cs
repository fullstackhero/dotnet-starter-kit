namespace FSH.Framework.Core.CQRS;
public interface IQueryDispatcher
{
    /// <summary>
    /// Sends a query to its handler.
    /// </summary>
    Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResponse>;
}
