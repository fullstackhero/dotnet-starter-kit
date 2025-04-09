using FSH.Framework.Core.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.CQRS;
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);

        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return handler.HandleAsync(query, ct);
    }
}
