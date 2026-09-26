using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Framework.Shared.Persistence;
using Microsoft.Extensions.Localization;

namespace FSH.Framework.Web.Validation;

/// <summary>
/// Shared validator for types implementing IPagedQuery.
/// Use with Include() to add pagination validation rules to your validator.
/// </summary>
/// <example>
/// public class MyQueryValidator : AbstractValidator&lt;MyQuery&gt;
/// {
///     public MyQueryValidator(IStringLocalizer&lt;SharedResources&gt; localizer)
///     {
///         Include(new PagedQueryValidator&lt;MyQuery&gt;(localizer));
///         // Add additional rules...
///     }
/// }
/// </example>
public sealed class PagedQueryValidator<T> : AbstractValidator<T>
    where T : IPagedQuery
{
    public PagedQueryValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(q => q.PageNumber)
            .GreaterThan(0)
            .When(q => q.PageNumber.HasValue)
            .WithMessage(_ => localizer["Validation.PageNumberMinimum"]);

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .When(q => q.PageSize.HasValue)
            .WithMessage(_ => localizer["Validation.PageSizeRange"]);

        RuleFor(q => q.Sort)
            .MaximumLength(200)
            .When(q => !string.IsNullOrEmpty(q.Sort))
            .WithMessage(_ => localizer["Validation.SortMaxLength"]);
    }
}