using FluentValidation;
using MediatR;

namespace FSH.WebApi.Infrastructure.Common.Validation;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(request))))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        return failures.Count > 0
            ? throw new ValidationException(failures)
            : await next();
    }
}