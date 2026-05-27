namespace FSH.Modules.Identity.Contracts.DTOs;

public record TokenDto(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);