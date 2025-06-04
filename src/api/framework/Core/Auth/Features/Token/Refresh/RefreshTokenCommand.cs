using MediatR;
using FSH.Framework.Core.Auth.Dtos;

namespace FSH.Framework.Core.Auth.Features.Token.Refresh;

public class RefreshTokenCommand : IRequest<TokenResponseDto>
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
} 