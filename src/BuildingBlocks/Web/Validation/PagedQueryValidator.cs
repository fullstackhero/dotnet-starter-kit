using FluentValidation;
using FSH.Framework.Shared.Persistence;

namespace FSH.Framework.Web.Validation;

/// <summary>
/// Shared validator for types implementing IPagedQuery.
/// Use with Include() to add pagination validation rules to your validator.
/// </summary>
/// <example>
/// public class MyQueryValidator : AbstractValidator&lt;MyQuery&gt;
/// {
///     public MyQueryValidator()
///     {
///         Include(new PagedQueryValidator&lt;MyQuery&gt;());
///         // Add additional rules...
///     }
/// }
/// </example>
public sealed class PagedQueryValidator<T> : AbstractValidator<T>
    where T : IPagedQuery
{
    public PagedQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThan(0)
            .When(q => q.PageNumber.HasValue)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .When(q => q.PageSize.HasValue)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(q => q.Sort)
            .MaximumLength(200)
            .When(q => !string.IsNullOrEmpty(q.Sort))
            .WithMessage("Sort expression must not exceed 200 characters.");
    }
}