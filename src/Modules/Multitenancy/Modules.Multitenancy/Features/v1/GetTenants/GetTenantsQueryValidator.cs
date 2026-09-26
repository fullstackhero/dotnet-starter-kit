using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Framework.Web.Validation;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenants;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Multitenancy.Features.v1.GetTenants;

public sealed class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsQueryValidator(IStringLocalizer<SharedResources> localizer)
    {
        Include(new PagedQueryValidator<GetTenantsQuery>(localizer));
    }
}