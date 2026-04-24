namespace FSH.Modules.Identity.Contracts.DTOs;

/// <summary>
/// Returned from the enroll endpoint. <see cref="SharedKey"/> is the base32-encoded TOTP
/// secret (display to users who can't scan the QR). <see cref="AuthenticatorUri"/> is the
/// standard otpauth:// URI suitable for rendering as a QR code.
/// </summary>
public sealed record TwoFactorEnrollmentResponse(string SharedKey, string AuthenticatorUri);
