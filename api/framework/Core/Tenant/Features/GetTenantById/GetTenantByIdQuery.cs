using FSH.Framework.Core.Tenant.Dtos;
using MediatR;

namespace FSH.Framework.Core.Tenant.Features.GetTenants;
public record GetTenantByIdQuery(string TenantId) : IRequest<TenantDetail>;
