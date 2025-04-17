namespace FSH.Framework.Identity.Contracts.v1.Tokens.TokenGeneration;
public sealed record TokenGenerationCommandResponse(
    string Token,
    string RefreshToken,
    DateTime RefreshTokenExpiryTime);