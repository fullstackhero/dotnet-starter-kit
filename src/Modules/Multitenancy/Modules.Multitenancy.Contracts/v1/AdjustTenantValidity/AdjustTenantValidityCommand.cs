using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;

/// <summary>
/// Operator override that sets a tenant's validity to an explicit date with no billing side-effect
/// (no subscription, no invoice, no renewal event). Intended for comps, support extensions, or
/// immediate expiry. May move the date backward, unlike renewal.
/// </summary>
public sealed record AdjustTenantValidityCommand(string TenantId, DateTime ValidUpto)
    : ICommand<AdjustTenantValidityCommandResponse>;
