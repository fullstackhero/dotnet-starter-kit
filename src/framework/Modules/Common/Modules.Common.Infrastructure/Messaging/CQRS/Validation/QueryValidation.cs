using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Infrastructure.Messaging.CQRS.Validation;
internal sealed class QueryValidation : IQueryDispatcher
{
    private readonly IQueryDispatcher _inner;
    private readonly IServiceProvider _serviceProvider;

    public QueryValidation(IQueryDispatcher inner, IServiceProvider serviceProvider)
    {
        _inner = inner;
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(query, _serviceProvider, ct);
        return await _inner.SendAsync(query, ct);
    }
}