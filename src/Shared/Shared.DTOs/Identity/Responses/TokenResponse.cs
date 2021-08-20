namespace DN.WebApi.Shared.DTOs.Identity.Responses
{
    public record TokenResponse(string Token, DateTime TokenExpiryTime, string RefreshToken, DateTime RefreshTokenExpiryTime);
}