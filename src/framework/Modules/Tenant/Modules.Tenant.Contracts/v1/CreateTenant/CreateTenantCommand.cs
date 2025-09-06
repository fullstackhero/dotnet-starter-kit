using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Contracts.v1.CreateTenant;
public sealed record CreateTenantCommand(
    string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string? Issuer) : ICommand<CreateTenantCommandResponse>;