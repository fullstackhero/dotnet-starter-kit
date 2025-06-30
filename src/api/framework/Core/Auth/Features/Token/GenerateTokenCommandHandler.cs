using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using Microsoft.Extensions.Options;
using FSH.Framework.Core.Auth.Jwt;

namespace FSH.Framework.Core.Auth.Features.Token.Generate;

public class GenerateTokenCommandHandler : IRequestHandler<GenerateTokenCommand, TokenGenerationResult>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public GenerateTokenCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IJwtTokenGenerator jwt,
        IOptions<JwtOptions> jwtOptions)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
        _jwtOptions = jwtOptions;
    }

    public async Task<TokenGenerationResult> Handle(GenerateTokenCommand request, CancellationToken cancellationToken)
    {
        var (isValid, user) = await _users.ValidatePasswordAndGetByTcknAsync(request.Tckn, request.Password);
        
        if (!isValid || user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!string.Equals(user.Status, "ACTIVE", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Account is not active");
        }

        var roles = await _users.GetUserRolesAsync(user.Id);

        var token = _jwt.GenerateToken(
            user.Id, 
            user.Email.Value, 
            user.Username, 
            user.FirstName, 
            user.LastName, 
            user.PhoneNumber.Value, 
            user.ProfessionId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, 
            user.Status, 
            roles);

        var refreshToken = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenExpirationInDays);

        await _refreshTokens.SaveAsync(user.Id, refreshToken, refreshTokenExpiry);

        return new TokenGenerationResult
        {
            Token = token,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = refreshTokenExpiry
        };
    }
}