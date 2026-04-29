using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Tokens.RefreshToken;

// Token is the (possibly expired) access token. It is optional — when present, the
// handler cross-checks the access-token subject against the refresh-token subject as
// an additional safeguard. When absent, refresh proceeds on the strength of the
// refresh-token validation alone.
public record RefreshTokenCommand(string? Token, string RefreshToken)
    : ICommand<RefreshTokenCommandResponse>;