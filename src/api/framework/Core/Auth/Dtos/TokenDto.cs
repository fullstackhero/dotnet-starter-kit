using System;

namespace FSH.Framework.Core.Auth.Dtos;

public class TokenDto
{
    public string Token { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime RefreshTokenExpiryTime { get; set; }
}