using FluentValidation;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByTrace;
using FSH.Modules.Auditing.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Auditing.Features.v1.GetAuditsByTrace;

public sealed class GetAuditsByTraceQueryValidator : AbstractValidator<GetAuditsByTraceQuery>
{
    public GetAuditsByTraceQueryValidator(IStringLocalizer<AuditingResources> localizer)
    {
        RuleFor(q => q.TraceId)
            .NotEmpty();

        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage(_ => localizer["Validation.DateRangeOrder"]);
    }
}
