namespace FSH.WebApi.Application.Multitenancy;

public class TenantByNameSpec : Specification<Tenant>, ISingleResultSpecification
{
    public TenantByNameSpec(string name) =>
        Query.Where(tenant => tenant.Name == name);
}