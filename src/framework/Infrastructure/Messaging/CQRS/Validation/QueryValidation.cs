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

    public async Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResponse>
    {
        var typedValidators = _validators.OfType<IValidator<TQuery>>();
        await ValidationHelper.ValidateAsync(query, typedValidators, ct);

        return await _inner.SendAsync<TQuery, TResponse>(query, ct);
    }
}
