using MediatR;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Command for completing email update after verification.
/// SECURITY: Updates email address only after successful verification.
/// This is the second step in the secure email update process.
/// </summary>
public class VerifyEmailUpdateCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string NewEmail { get; set; } = default!;
    public string VerificationCode { get; set; } = default!;
} 