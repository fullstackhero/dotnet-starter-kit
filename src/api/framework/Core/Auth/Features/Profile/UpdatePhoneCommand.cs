using MediatR;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Command for initiating phone update process.
/// SECURITY: Only sends verification code, does NOT update phone immediately.
/// NOTE: Verification code generation and SMS service integration implemented in handler.
/// </summary>
public class UpdatePhoneCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string NewPhoneNumber { get; set; } = default!;
} 