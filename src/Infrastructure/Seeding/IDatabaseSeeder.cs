using FSH.WebAPI.Domain.Multitenancy;

namespace FSH.WebAPI.Infrastructure.Seeding;

public interface IDatabaseSeeder
{
    void Initialize(Tenant tenant);
}