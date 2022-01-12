using FSH.WebApi.Domain.Multitenancy;

namespace FSH.WebApi.Infrastructure.Seeding;

public interface IDatabaseSeeder
{
    void Initialize(Tenant tenant);
}