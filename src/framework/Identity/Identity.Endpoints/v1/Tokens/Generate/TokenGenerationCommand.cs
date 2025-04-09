using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Generate;
public sealed record TokenGenerationCommand(string Email, string Password)
    : ICommand<TokenGenerationResponse>;
