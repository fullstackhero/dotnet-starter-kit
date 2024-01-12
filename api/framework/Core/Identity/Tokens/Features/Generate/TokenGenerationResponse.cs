namespace FSH.Framework.Core.Identity.Tokens.Features.Generate;
public record TokenGenerationResponse(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);
