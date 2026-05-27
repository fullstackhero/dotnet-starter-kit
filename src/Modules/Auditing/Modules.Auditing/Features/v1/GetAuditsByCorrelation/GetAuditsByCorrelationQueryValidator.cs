using FluentValidation;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByCorrelation;

namespace FSH.Modules.Auditing.Features.v1.GetAuditsByCorrelation;

public sealed class GetAuditsByCorrelationQueryValidator : AbstractValidator<GetAuditsByCorrelationQuery>
{
    public GetAuditsByCorrelationQueryValidator()
    {
        RuleFor(q => q.CorrelationId)
            .NotEmpty();

        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage("FromUtc must be less than or equal to ToUtc.");
    }
}