using MediatR;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Command for completing phone update after verification.
/// SECURITY: Updates phone number only after successful verification.
/// This is the second step in the secure phone update process.
/// </summary>
public class VerifyPhoneUpdateCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string NewPhoneNumber { get; set; } = default!;
    public string VerificationCode { get; set; } = default!;
} 