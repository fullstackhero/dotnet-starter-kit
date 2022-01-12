namespace FSH.WebApi.Infrastructure.Multitenancy;

public interface ICurrentTenantInitializer
{
    void SetCurrentTenant(string tenant);
}