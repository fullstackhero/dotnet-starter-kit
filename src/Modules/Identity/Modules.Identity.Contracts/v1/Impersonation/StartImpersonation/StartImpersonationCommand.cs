using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

public sealed record StartImpersonationCommand(
    string TargetUserId,
    string TargetTenantId,
    string? Reason)
    : ICommand<ImpersonationResponse>;
