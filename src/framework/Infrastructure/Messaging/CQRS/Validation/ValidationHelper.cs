using FluentValidation;

namespace FSH.Framework.Infrastructure.Messaging.CQRS.Validation;

internal static class ValidationHelper
{
    public static async Task ValidateAsync<TRequest>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return;

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);
    }
}
