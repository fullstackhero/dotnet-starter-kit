using System;
using MediatR;

namespace FSH.Framework.Core.Auth.Features.Token.Generate;

public class GenerateTokenCommand : IRequest<TokenGenerationResult>
{
    public string Tckn { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class TokenGenerationResult
{
    public string Token { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime RefreshTokenExpiryTime { get; set; }
}