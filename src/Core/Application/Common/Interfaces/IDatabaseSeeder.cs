using DN.WebApi.Domain.Multitenancy;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IDatabaseSeeder
{
    void Initialize(Tenant tenant);
}