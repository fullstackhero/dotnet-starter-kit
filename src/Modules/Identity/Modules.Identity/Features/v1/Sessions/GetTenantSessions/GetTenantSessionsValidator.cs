using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Sessions.GetTenantSessions;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Sessions.GetTenantSessions;

public sealed class GetTenantSessionsValidator : AbstractValidator<GetTenantSessionsQuery>
{
    public GetTenantSessionsValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage(_ => localizer["Validation.PageNumberMinimum"]);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage(_ => localizer["Validation.PageSizeMinimum"]);
    }
}
