using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Contracts.v1.Tokens.RefreshToken;

namespace FSH.Framework.Identity.Core.Tokens;
public record RefreshTokenCommand(string Token, string RefreshToken)
    : ICommand<RefreshTokenCommandResponse>;