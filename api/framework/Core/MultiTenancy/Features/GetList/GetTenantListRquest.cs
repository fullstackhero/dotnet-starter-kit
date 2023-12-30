using MediatR;

namespace FSH.Framework.Core.MultiTenancy.Features.GetList;
public record GetTenantListRquest() : IRequest<List<TenantDto>>;
