using FluentValidation;
using MediatR;

namespace FSH.WebApi.Infrastructure.Common.Validation;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, RequestHandlerDelegate<TResponse> next)
    {
        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(
                v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        return failures.Count > 0
            ? throw new ValidationException(failures)
            : await next();
    }
}