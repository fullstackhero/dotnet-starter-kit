using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

// DurationMinutes: caller-requested impersonation token lifetime in minutes.
// Server caps at StartImpersonationCommandValidator.MaxImpersonationMinutes
// (currently 60). Null falls back to JwtOptions.AccessTokenMinutes.
public sealed record StartImpersonationCommand(
    string TargetUserId,
    string TargetTenantId,
    string? Reason,
    int? DurationMinutes = null)
    : ICommand<ImpersonationResponse>;
