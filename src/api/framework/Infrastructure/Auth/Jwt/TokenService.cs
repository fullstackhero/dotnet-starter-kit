using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Features.Login;
using FSH.Framework.Core.Auth.Jwt;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Common.Models;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Auth.Jwt;

public class TokenService : ITokenService
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public TokenService(
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtOptions = jwtOptions;
    }

    public async Task<Result<TokenGenerationResult>> GenerateTokenAsync(AppUser user, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        try
        {
            var token = _jwtTokenGenerator.GenerateToken(
                user.Id,
                user.Email.Value,
                user.Username,
                user.FirstName,
                user.LastName,
                user.PhoneNumber.Value,
                user.Profession,
                user.Status,
                roles);

            var refreshToken = Guid.NewGuid().ToString();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenExpirationInDays);

            await _refreshTokenRepository.SaveAsync(user.Id, refreshToken, refreshTokenExpiry);

            return Result<TokenGenerationResult>.Success(new TokenGenerationResult
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiry
            });
        }
        catch (Exception ex)
        {
            return Result<TokenGenerationResult>.Failure($"Failed to generate token: {ex.Message}");
        }
    }
} 