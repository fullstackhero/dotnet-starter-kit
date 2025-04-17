using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Contracts.v1.GetTenants;
public sealed record GetTenantsQuery : IQuery<GetTenantsQueryResponse>;