using FluentValidation;
using FluentValidation.Results;
using System.Linq;

namespace DN.WebApi.Application.Validators
{
    public class CustomValidator<T> : AbstractValidator<T>
    {
        public override ValidationResult Validate(ValidationContext<T> context)
        {
            var validationResult = base.Validate(context);
            if (!validationResult.IsValid)
            {
                var failures = validationResult.Errors.ToList();
                if (failures.Count != 0)
                    throw new Exceptions.ValidationException(failures.Select(a => a.ErrorMessage).ToList());
            }

            return validationResult;
        }
    }
}