using FluentValidation;
using MediatR;

namespace FSH.WebApi.Infrastructure.Common.Validation;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IServiceProvider _sp;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, IServiceProvider sp) =>
        (_validators, _sp) = (validators, sp);

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var context = new ValidationContext<TRequest>(request);
        context.SetServiceProvider(_sp); // to be able to use InjectValidator()

        var results = await Task.WhenAll(
            _validators.Select(
                v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        return failures.Count > 0
            ? throw new ValidationException(failures)
            : await next();
    }
}