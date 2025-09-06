using MediatR;

namespace FSH.Framework.Core.Tenant.Features.ActivateTenant;
public record ActivateTenantCommand(string TenantId) : IRequest<ActivateTenantResponse>;
