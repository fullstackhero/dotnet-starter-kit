using MediatR;

namespace FSH.Framework.Core.Auth.Features.Profile;

/// <summary>
/// Command for initiating email update process.
/// SECURITY: Only sends verification code, does NOT update email immediately.
/// TODO: Add verification code generation and email service integration.
/// </summary>
public class UpdateEmailCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string NewEmail { get; set; } = default!;
} 