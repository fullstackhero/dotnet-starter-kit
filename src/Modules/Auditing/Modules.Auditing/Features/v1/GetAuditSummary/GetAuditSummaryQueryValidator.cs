using FluentValidation;
using FSH.Modules.Auditing.Contracts.v1.GetAuditSummary;

namespace FSH.Modules.Auditing.Features.v1.GetAuditSummary;

public sealed class GetAuditSummaryQueryValidator : AbstractValidator<GetAuditSummaryQuery>
{
    public GetAuditSummaryQueryValidator()
    {
        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage("FromUtc must be less than or equal to ToUtc.");

        RuleFor(q => q)
            .Must(q =>
                !q.FromUtc.HasValue
                || !q.ToUtc.HasValue
                || (q.ToUtc.Value - q.FromUtc.Value) <= GetAuditSummaryQueryHandler.MaxWindow)
            .WithMessage($"Audit summary window cannot exceed {GetAuditSummaryQueryHandler.MaxWindow.TotalDays:0} days.");
    }
}
