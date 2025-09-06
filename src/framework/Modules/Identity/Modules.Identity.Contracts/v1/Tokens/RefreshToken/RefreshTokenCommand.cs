using FSH.Framework.Identity.Contracts.v1.Tokens.RefreshToken;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Identity.Core.Tokens;
public record RefreshTokenCommand(string Token, string RefreshToken)
    : ICommand<RefreshTokenCommandResponse>;