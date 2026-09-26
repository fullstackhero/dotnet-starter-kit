using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Framework.Web.Validation;
using FSH.Modules.Auditing.Contracts.v1.GetAudits;
using FSH.Modules.Auditing.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Auditing.Features.v1.GetAudits;

public sealed class GetAuditsQueryValidator : AbstractValidator<GetAuditsQuery>
{
    public GetAuditsQueryValidator(
        IStringLocalizer<SharedResources> localizer,
        IStringLocalizer<AuditingResources> auditLocalizer)
    {
        Include(new PagedQueryValidator<GetAuditsQuery>(localizer));

        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage(_ => auditLocalizer["Validation.DateRangeOrder"]);

        // Reject oversized windows up-front (user sees a 400, not a silent clamp). The handler
        // still clamps as defence in depth (e.g. when only one endpoint is supplied).
        RuleFor(q => q)
            .Must(q =>
                !q.FromUtc.HasValue
                || !q.ToUtc.HasValue
                || (q.ToUtc.Value - q.FromUtc.Value) <= GetAuditsQueryHandler.MaxWindow)
            // MaxWindowDays, not MaxWindow.TotalDays: the localizer formats arguments with
            // string.Format under the current culture, and a double in a localized message is
            // culture-sensitive by construction. An int cannot render a decimal separator.
            .WithMessage(_ => auditLocalizer["Validation.WindowExceeded", GetAuditsQueryHandler.MaxWindowDays]);
    }
}
