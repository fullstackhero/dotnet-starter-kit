using FSH.Framework.Core.Tenant.Dtos;
using MediatR;

namespace FSH.Framework.Core.Tenant.Features.GetTenants.v1;
public sealed class GetTenantsQuery : IRequest<List<TenantDetail>>;