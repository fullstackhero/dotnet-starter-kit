using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.TwoFactor;

/// <summary>
/// Disable two-factor authentication for the current user. Requires the current password
/// as confirmation so a stolen access token alone can't downgrade account security.
/// </summary>
public sealed record DisableTwoFactorCommand(string CurrentPassword) : ICommand<bool>;
