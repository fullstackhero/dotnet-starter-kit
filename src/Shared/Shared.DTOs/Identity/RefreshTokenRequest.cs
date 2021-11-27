namespace DN.WebApi.Shared.DTOs.Identity;

public record RefreshTokenRequest(string Token, string RefreshToken);