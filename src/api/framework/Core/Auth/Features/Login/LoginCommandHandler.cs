using MediatR;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace FSH.Framework.Core.Auth.Features.Login;

public interface ITokenService
{
    Task<Result<TokenGenerationResult>> GenerateTokenAsync(AppUser user, IReadOnlyList<string> roles, CancellationToken cancellationToken);
}

public class TokenGenerationResult
{
    public string AccessToken { get; init; } = default!;
    public string RefreshToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            AppUser? user = null;
            bool isPasswordValid = false;

            // Determine if input is TCKN (11 digits) or Member Number
            if (request.TcknOrMemberNumber.Length == 11 && request.TcknOrMemberNumber.All(char.IsDigit))
            {
                // Try login with TCKN
                var (isValid, foundUser) = await _userRepository.ValidatePasswordAndGetByTcknAsync(
                    request.TcknOrMemberNumber,
                    request.Password.Value);
                
                isPasswordValid = isValid;
                user = foundUser;
            }
            else
            {
                // Try login with Member Number
                var (isValid, foundUser) = await _userRepository.ValidatePasswordAndGetByMemberNumberAsync(
                    request.TcknOrMemberNumber,
                request.Password.Value);

                isPasswordValid = isValid;
                user = foundUser;
            }

            if (!isPasswordValid || user == null)
            {
                return Result<LoginResponseDto>.Failure("Geçersiz kimlik bilgileri. Lütfen TC Kimlik No/Üye No ve şifrenizi kontrol ediniz.");
            }

            var roles = await _userRepository.GetUserRolesAsync(user.Id);
            var tokenResult = await _tokenService.GenerateTokenAsync(user, roles, cancellationToken);

            if (!tokenResult.IsSuccess)
            {
                return Result<LoginResponseDto>.Failure("Failed to generate authentication token");
            }

            return Result<LoginResponseDto>.Success(new LoginResponseDto
            {
                UserId = user.Id,
                Email = user.Email.Value,
                Username = user.Username,
                AccessToken = tokenResult.Value.AccessToken,
                RefreshToken = tokenResult.Value.RefreshToken,
                ExpiresAt = tokenResult.Value.ExpiresAt,
                Roles = new ReadOnlyCollection<string>(roles.ToList())
            });
        }
        catch (Exception ex)
        {
            return Result<LoginResponseDto>.Failure("An error occurred during login. Please try again later.");
        }
    }
} 