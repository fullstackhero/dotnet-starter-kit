using FSH.Framework.Core.Tenant.Dtos;
using MediatR;

namespace FSH.Framework.Core.Tenant.Features.v1.GetTenants;
public sealed class GetTenantsQuery : IRequest<List<TenantDetail>>;