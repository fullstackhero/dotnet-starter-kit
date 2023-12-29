using MediatR;

namespace FSH.Framework.Core.MultiTenancy.Features.Creation;
public record TenantCreationCommand(
    string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string? Issuer) : IRequest<string>;
