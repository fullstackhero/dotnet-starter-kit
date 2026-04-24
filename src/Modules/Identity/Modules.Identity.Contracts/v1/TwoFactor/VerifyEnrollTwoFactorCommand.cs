using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.TwoFactor;

/// <summary>
/// Verify the 6-digit TOTP code from the user's authenticator app. On success, two-factor
/// authentication is enabled on the user — subsequent logins must include a valid code.
/// </summary>
public sealed record VerifyEnrollTwoFactorCommand(string Code) : ICommand<bool>;
