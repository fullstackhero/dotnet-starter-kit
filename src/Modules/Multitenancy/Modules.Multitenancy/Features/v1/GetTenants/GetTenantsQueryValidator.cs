using FluentValidation;
using FSH.Framework.Web.Validation;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenants;

namespace FSH.Modules.Multitenancy.Features.v1.GetTenants;

public sealed class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsQueryValidator()
    {
        Include(new PagedQueryValidator<GetTenantsQuery>());
    }
}