using FluentValidation;
using Mediator;

namespace FSH.Framework.Web.Mediator.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IValidator<TMessage>> _validators = validators;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Any())
        {
            var context = new ValidationContext<TMessage>(message);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count > 0)
                throw new ValidationException(failures);
        }
        return await next(message, cancellationToken);
    }
}