using FluentValidation;
using FSH.Modules.Auditing.Contracts.v1.GetAuditSummary;
using FSH.Modules.Auditing.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Auditing.Features.v1.GetAuditSummary;

public sealed class GetAuditSummaryQueryValidator : AbstractValidator<GetAuditSummaryQuery>
{
    public GetAuditSummaryQueryValidator(IStringLocalizer<AuditingResources> localizer)
    {
        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage(_ => localizer["Validation.DateRangeOrder"]);

        RuleFor(q => q)
            .Must(q =>
                !q.FromUtc.HasValue
                || !q.ToUtc.HasValue
                || (q.ToUtc.Value - q.FromUtc.Value) <= GetAuditSummaryQueryHandler.MaxWindow)
            // MaxWindowDays, not MaxWindow.TotalDays: the localizer formats arguments with
            // string.Format under the current culture, and a double in a localized message is
            // culture-sensitive by construction. An int cannot render a decimal separator.
            .WithMessage(_ => localizer["Validation.SummaryWindowExceeded", GetAuditSummaryQueryHandler.MaxWindowDays]);
    }
}
