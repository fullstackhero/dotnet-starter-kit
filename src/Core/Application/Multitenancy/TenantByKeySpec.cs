using Ardalis.Specification;
using DN.WebApi.Domain.Multitenancy;

namespace DN.WebApi.Application.Multitenancy;

public class TenantByKeySpec : Specification<Tenant>, ISingleResultSpecification
{
    public TenantByKeySpec(string key) =>
        Query.Where(tenant => tenant.Key == key);
}