using FluentValidation;
using FluentValidation.Results;

namespace DN.WebApi.Application.Common.Validators;

public class CustomValidator<T> : AbstractValidator<T>
{
    public override ValidationResult Validate(ValidationContext<T> context)
    {
        var validationResult = base.Validate(context);
        if (!validationResult.IsValid)
        {
            var failures = validationResult.Errors.ToList();
            if (failures.Count != 0)
                throw new Exceptions.ValidationException(failures.ConvertAll(a => a.ErrorMessage));
        }

        return validationResult;
    }
}