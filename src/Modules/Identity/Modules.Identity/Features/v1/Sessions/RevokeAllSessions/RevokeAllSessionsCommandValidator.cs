using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Sessions.RevokeAllSessions;

namespace FSH.Modules.Identity.Features.v1.Sessions.RevokeAllSessions;

public sealed class RevokeAllSessionsCommandValidator : AbstractValidator<RevokeAllSessionsCommand>
{
    public RevokeAllSessionsCommandValidator()
    {
        // ExceptSessionId is optional - no validation required
        // This validator exists for consistency and potential future validation rules
    }
}
