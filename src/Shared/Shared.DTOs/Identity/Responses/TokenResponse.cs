using System;

namespace DN.WebApi.Shared.DTOs.Identity.Responses
{
    public record TokenResponse(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);
}