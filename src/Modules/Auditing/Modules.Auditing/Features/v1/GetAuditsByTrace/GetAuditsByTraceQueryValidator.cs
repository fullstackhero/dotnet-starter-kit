using FluentValidation;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByTrace;

namespace FSH.Modules.Auditing.Features.v1.GetAuditsByTrace;

public sealed class GetAuditsByTraceQueryValidator : AbstractValidator<GetAuditsByTraceQuery>
{
    public GetAuditsByTraceQueryValidator()
    {
        RuleFor(q => q.TraceId)
            .NotEmpty();

        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage("FromUtc must be less than or equal to ToUtc.");
    }
}