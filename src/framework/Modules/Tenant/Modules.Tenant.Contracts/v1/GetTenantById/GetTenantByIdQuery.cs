using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Tenant.Contracts.Dtos;

namespace FSH.Framework.Tenant.Contracts.v1.GetTenantById;
public sealed record GetTenantByIdQuery(string TenantId) : IQuery<TenantDto>;