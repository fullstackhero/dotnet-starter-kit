using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Messaging.CQRS.Validation;

internal static class ValidationHelper
{
    public static async Task ValidateAsync<T>(T request, IServiceProvider provider, CancellationToken ct = default)
    {
        var requestType = request.GetType();
        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
        var validators = provider.GetServices(validatorType).Cast<IValidator>().ToList();

        if (validators.Count == 0) return;

        var contextType = typeof(ValidationContext<>).MakeGenericType(requestType);
        var context = Activator.CreateInstance(contextType, request)!;

        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var validateAsyncMethod = validator.GetType()
                .GetMethod("ValidateAsync", new[] { contextType, typeof(CancellationToken) })!;

            var task = (Task<FluentValidation.Results.ValidationResult>)
                validateAsyncMethod.Invoke(validator, new[] { context, ct })!;

            var result = await task;
            failures.AddRange(result.Errors.Where(f => f != null));
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);
    }
}