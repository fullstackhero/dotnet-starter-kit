namespace FSH.Modules.Identity.Contracts.DTOs;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    DateTime AccessTokenExpiresAt);