namespace FSH.Framework.Core.Messaging.CQRS;

/// <summary>
/// Handles a query and returns a result.
/// </summary>
/// <typeparam name="TQuery">Type of query</typeparam>
/// <typeparam name="TResponse">Type of response</typeparam>
public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}