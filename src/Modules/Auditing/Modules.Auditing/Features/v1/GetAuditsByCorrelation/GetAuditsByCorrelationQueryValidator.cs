using FluentValidation;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByCorrelation;
using FSH.Modules.Auditing.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Auditing.Features.v1.GetAuditsByCorrelation;

public sealed class GetAuditsByCorrelationQueryValidator : AbstractValidator<GetAuditsByCorrelationQuery>
{
    public GetAuditsByCorrelationQueryValidator(IStringLocalizer<AuditingResources> localizer)
    {
        RuleFor(q => q.CorrelationId)
            .NotEmpty();

        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage(_ => localizer["Validation.DateRangeOrder"]);
    }
}
