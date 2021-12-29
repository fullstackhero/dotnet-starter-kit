using FluentValidation;
using FluentValidation.Results;

namespace DN.WebApi.Application.Common.Validation;

public class CustomValidator<T> : AbstractValidator<T>
{
    public override ValidationResult Validate(ValidationContext<T> context)
    {
        var validationResult = base.Validate(context);
        if (!validationResult.IsValid)
        {
            var failures = validationResult.Errors.ToList();
            if (failures.Count != 0)
            {
                var validationErrors = validationResult.Errors
                    .ToLookup(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(l => l.Key, l => l.ToList());
                throw new Exceptions.ValidationException(
                    failures.ConvertAll(a => a.ErrorMessage),
                    validationErrors);
            }
        }

        return validationResult;
    }
}