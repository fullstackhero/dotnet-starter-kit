namespace FSH.Framework.Core.Messaging.CQRS;
public interface IQueryDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
}