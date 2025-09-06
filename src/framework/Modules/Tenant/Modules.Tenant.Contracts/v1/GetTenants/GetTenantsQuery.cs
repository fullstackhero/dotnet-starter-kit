using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Tenant.Contracts.Dtos;

namespace FSH.Framework.Tenant.Contracts.v1.GetTenants;
public sealed record GetTenantsQuery : IQuery<IReadOnlyCollection<TenantDto>>;