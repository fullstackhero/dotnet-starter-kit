namespace FSH.Framework.Identity.Core.Tokens;
public record TokenDto(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);