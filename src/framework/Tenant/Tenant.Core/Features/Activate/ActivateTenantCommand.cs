using MediatR;

namespace FSH.Framework.Tenant.Core.Features.Activate;
public record ActivateTenantCommand(string TenantId) : IRequest<ActivateTenantResponse>;
