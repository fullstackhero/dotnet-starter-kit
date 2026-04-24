using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.TwoFactor;

/// <summary>
/// Begin TOTP enrollment for the current user. Generates (or replaces) the user's
/// authenticator shared secret and returns it along with an otpauth:// URI suitable
/// for rendering as a QR code. Two-factor is NOT enabled until the caller confirms
/// the code via <see cref="VerifyEnrollTwoFactorCommand"/>.
/// </summary>
public sealed record EnrollTwoFactorCommand : ICommand<TwoFactorEnrollmentResponse>;
