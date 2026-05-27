using FSH.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.GetTenantMigrations;

public sealed record GetTenantMigrationsQuery : IQuery<IReadOnlyCollection<TenantMigrationStatusDto>>;