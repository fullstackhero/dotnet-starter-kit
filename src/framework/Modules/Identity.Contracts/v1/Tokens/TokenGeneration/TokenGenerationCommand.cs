using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Identity.Contracts.v1.Tokens.TokenGeneration;
public record TokenGenerationCommand(string Email, string Password, string IpAddress)
    : ICommand<TokenGenerationCommandResponse>;
