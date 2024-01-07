using MediatR;

namespace FSH.Framework.Core.Tenant.Features.RegisterTenant;
public sealed record RegisterTenantCommand(string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string? Issuer) : IRequest<string>;