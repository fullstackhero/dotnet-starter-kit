using FSH.Framework.Core.Tenant.Dtos;
using MediatR;

namespace FSH.Framework.Core.Tenant.Features.GetTenantById;
public record GetTenantByIdQuery(string TenantId) : IRequest<TenantDetail>;
