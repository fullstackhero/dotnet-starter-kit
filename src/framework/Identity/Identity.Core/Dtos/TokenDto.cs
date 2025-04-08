namespace FSH.Framework.Identity.Core.Dtos;
public record TokenDto(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);
