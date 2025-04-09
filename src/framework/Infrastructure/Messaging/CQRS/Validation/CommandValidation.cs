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

    public async Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResponse>
    {
        var typedValidators = _validators.OfType<IValidator<TCommand>>();
        await ValidationHelper.ValidateAsync(command, typedValidators, ct);

        return await _inner.SendAsync<TCommand, TResponse>(command, ct);
    }
}
