namespace FSH.WebAPI.Infrastructure.Multitenancy;

public interface ICurrentTenantInitializer
{
    void SetCurrentTenant(string tenant);
}