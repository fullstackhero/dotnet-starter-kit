using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace FSH.Framework.Web.Mediator.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    // Materialize once; this behavior runs for every command/query in the system and the common
    // case is zero or one validator, so avoid the per-request Task.WhenAll array allocation.
    private readonly IValidator<TMessage>[] _validators;

    public ValidationBehavior(IEnumerable<IValidator<TMessage>> validators)
        => _validators = validators as IValidator<TMessage>[] ?? validators.ToArray();

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Length > 0)
        {
            var context = new ValidationContext<TMessage>(message);
            List<ValidationFailure> failures;

            if (_validators.Length == 1)
            {
                var result = await _validators[0].ValidateAsync(context, cancellationToken).ConfigureAwait(false);
                failures = result.Errors;
            }
            else
            {
                var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
                failures = results.SelectMany(r => r.Errors).ToList();
            }

            if (failures.Count > 0)
                throw new ValidationException(failures);
        }

        return await next(message, cancellationToken).ConfigureAwait(false);
    }
}