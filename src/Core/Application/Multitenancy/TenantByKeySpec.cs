namespace FSH.WebApi.Application.Multitenancy;

public class TenantByKeySpec : Specification<Tenant>, ISingleResultSpecification
{
    public TenantByKeySpec(string key) =>
        Query.Where(tenant => tenant.Key == key);
}