using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Infrastructure.Messaging.CQRS.Validation;
internal class QueryValidation : IQueryDispatcher
{
    private readonly IQueryDispatcher _inner;
    private readonly IEnumerable<IValidator<object>> _validators;

    public QueryValidation(IQueryDispatcher inner, IEnumerable<IValidator<object>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(query, _validators, ct);
        return await _inner.SendAsync(query, ct);
    }
}
