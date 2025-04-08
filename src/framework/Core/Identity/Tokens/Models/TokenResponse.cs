namespace FSH.Framework.Core.Identity.Tokens.Models;
public record TokenResponse(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);
