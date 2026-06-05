using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

// DurationMinutes: requested token lifetime, capped server-side at
// StartImpersonationCommandValidator.MaxImpersonationMinutes (60); null → JwtOptions.AccessTokenMinutes.
public sealed record StartImpersonationCommand(
    string TargetUserId,
    string TargetTenantId,
    string? Reason,
    int? DurationMinutes = null)
    : ICommand<ImpersonationResponse>;
