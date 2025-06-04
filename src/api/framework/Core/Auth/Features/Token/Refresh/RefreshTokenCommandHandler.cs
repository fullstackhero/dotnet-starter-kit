using MediatR;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Dtos;
using Microsoft.Extensions.Options;
using FSH.Framework.Core.Auth.Jwt;

namespace FSH.Framework.Core.Auth.Features.Token.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public RefreshTokenCommandHandler(IRefreshTokenRepository refreshTokens, IUserRepository users, IJwtTokenGenerator jwt, IOptions<JwtOptions> jwtOptions)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _jwt = jwt;
        _jwtOptions = jwtOptions;
    }

    public async Task<TokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await _refreshTokens.ValidateAsync(request.RefreshToken);
        if (!isValid || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _users.GetByIdAsync(userId) ?? throw new UnauthorizedAccessException("User not found");
        if (user.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("Account is not active");
        }

        var roles = await _users.GetUserRolesAsync(userId);

        var newJwt = _jwt.GenerateToken(
            user.Id, 
            user.Email.Value, 
            user.Username, 
            user.FirstName, 
            user.LastName, 
            user.PhoneNumber.Value, 
            user.Profession, 
            user.Status, 
            roles);

        var newRefresh = Guid.NewGuid().ToString();
        var expiry = DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenExpirationInDays);

        await _refreshTokens.SaveAsync(userId, newRefresh, expiry);

        return new TokenResponseDto
        {
            Token = newJwt,
            RefreshToken = newRefresh,
            RefreshTokenExpiryTime = expiry
        };
    }
} 