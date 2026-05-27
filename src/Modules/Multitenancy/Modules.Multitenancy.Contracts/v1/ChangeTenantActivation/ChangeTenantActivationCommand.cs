using FSH.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;

public sealed record ChangeTenantActivationCommand(string TenantId, bool IsActive)
    : ICommand<TenantLifecycleResultDto>;