using Ardalis.Specification;
using DN.WebApi.Domain.Multitenancy;

namespace DN.WebApi.Application.Multitenancy;

public class TenantByIssuerSpec : Specification<Tenant>, ISingleResultSpecification
{
    public TenantByIssuerSpec(string issuer) =>
        Query.Where(tenant => tenant.Issuer == issuer);
}