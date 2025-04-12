using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Contracts.v1.GetTenantById;
public sealed record GetTenantByIdQuery(string TenantId) : IQuery<GetTenantByIdQueryResponse>;