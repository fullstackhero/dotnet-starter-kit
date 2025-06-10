using MediatR;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Command for initiating phone update process.
/// SECURITY: Only sends verification code, does NOT update phone immediately.
/// TODO: Add verification code generation and SMS service integration.
/// </summary>
public class UpdatePhoneCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string NewPhoneNumber { get; set; } = default!;
} 