using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Usage;

namespace FSH.Modules.Billing.Features.v1.Usage.CaptureUsageSnapshots;

public sealed class CaptureUsageSnapshotsCommandValidator : AbstractValidator<CaptureUsageSnapshotsCommand>
{
    public CaptureUsageSnapshotsCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12);
    }
}
