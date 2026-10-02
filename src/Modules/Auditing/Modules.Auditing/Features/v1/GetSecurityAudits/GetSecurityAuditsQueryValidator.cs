using FluentValidation;
using FSH.Modules.Auditing.Contracts.v1.GetSecurityAudits;
using FSH.Modules.Auditing.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Auditing.Features.v1.GetSecurityAudits;

public sealed class GetSecurityAuditsQueryValidator : AbstractValidator<GetSecurityAuditsQuery>
{
    public GetSecurityAuditsQueryValidator(IStringLocalizer<AuditingResources> localizer)
    {
        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage(_ => localizer["Validation.DateRangeOrder"]);
    }
}
