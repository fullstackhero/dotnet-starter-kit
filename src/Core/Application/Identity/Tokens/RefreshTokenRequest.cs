namespace DN.WebApi.Application.Identity.Tokens;

public record RefreshTokenRequest(string Token, string RefreshToken);