using FluentValidation;

namespace FSH.Framework.Infrastructure.Messaging.CQRS.Validation;

internal static class ValidationHelper
{
    public static async Task ValidateAsync(object request, IEnumerable<IValidator> validators, CancellationToken ct = default)
    {
        var requestType = request.GetType();
        var applicableValidators = validators
            .Where(v => v.GetType().GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                          i.GetGenericArguments()[0].IsAssignableFrom(requestType)))
            .ToList();

        if (applicableValidators.Count == 0) return;

        var contextType = typeof(ValidationContext<>).MakeGenericType(requestType);
        var context = Activator.CreateInstance(contextType, request)!;

        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in applicableValidators)
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
