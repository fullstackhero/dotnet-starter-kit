using MediatR;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ChangePasswordCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
} 