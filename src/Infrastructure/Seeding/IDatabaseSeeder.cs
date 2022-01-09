using DN.WebApi.Domain.Multitenancy;

namespace DN.WebApi.Infrastructure.Seeding;

public interface IDatabaseSeeder
{
    void Initialize(Tenant tenant);
}