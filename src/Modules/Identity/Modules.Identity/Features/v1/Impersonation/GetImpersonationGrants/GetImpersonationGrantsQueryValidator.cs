using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Impersonation.GetImpersonationGrants;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Impersonation.GetImpersonationGrants;

public sealed class GetImpersonationGrantsQueryValidator : AbstractValidator<GetImpersonationGrantsQuery>
{
    public const int MaxTake = 500;

    public GetImpersonationGrantsQueryValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(q => q.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxTake)
            .WithMessage(_ => localizer["Validation.ImpersonationTakeRange", MaxTake]);
    }
}
