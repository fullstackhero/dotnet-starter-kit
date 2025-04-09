namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Generate;
public sealed record TokenGenerationResponse(
    string Token,
    string RefreshToken,
    DateTime RefreshTokenExpiryTime);
