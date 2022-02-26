using FluentValidation.Validators;

namespace FSH.WebApi.Application.Common.Validation;

// SetValidator doesn't work when dealing with a nullable reference type
// Use this SetNonNullableValidator extension method instead
// For more info see https://github.com/FluentValidation/FluentValidation/issues/1648
public static class SetNonNullableValidatorExtension
{
    public static IRuleBuilderOptions<T, TProperty?> SetNonNullableValidator<T, TProperty>(this IRuleBuilder<T, TProperty?> ruleBuilder, IValidator<TProperty> validator, params string[] ruleSets)
    {
        var adapter = new NullableChildValidatorAdaptor<T, TProperty>(validator, validator.GetType())
        {
            RuleSets = ruleSets
        };

        return ruleBuilder.SetAsyncValidator(adapter);
    }

    private class NullableChildValidatorAdaptor<T, TProperty> : ChildValidatorAdaptor<T, TProperty>, IPropertyValidator<T, TProperty?>, IAsyncPropertyValidator<T, TProperty?>
    {
        public NullableChildValidatorAdaptor(IValidator<TProperty> validator, Type validatorType)
            : base(validator, validatorType)
        {
        }
#pragma warning disable RCS1132
        public override bool IsValid(ValidationContext<T> context, TProperty? value)
        {
            return base.IsValid(context, value!);
        }

        public override Task<bool> IsValidAsync(ValidationContext<T> context, TProperty? value, CancellationToken cancellation)
        {
            return base.IsValidAsync(context, value!, cancellation);
        }
#pragma warning restore RCS1132
    }
}