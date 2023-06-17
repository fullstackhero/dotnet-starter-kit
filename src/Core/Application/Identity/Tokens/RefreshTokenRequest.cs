namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Tokens;

public record RefreshTokenRequest(string Token, string RefreshToken);