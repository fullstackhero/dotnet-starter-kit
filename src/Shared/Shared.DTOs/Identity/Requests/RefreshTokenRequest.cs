namespace DN.WebApi.Shared.DTOs.Identity.Requests
{
    public record RefreshTokenRequest(string Token, string RefreshToken);
}