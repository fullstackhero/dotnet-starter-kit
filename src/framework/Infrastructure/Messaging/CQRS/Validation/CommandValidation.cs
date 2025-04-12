using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Infrastructure.Messaging.CQRS.Validation;
public class CommandValidation : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;
    private readonly IEnumerable<IValidator<object>> _validators;

    public CommandValidation(ICommandDispatcher inner, IEnumerable<IValidator<object>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(command, _validators, ct);
        return await _inner.SendAsync(command, ct);
    }
}
