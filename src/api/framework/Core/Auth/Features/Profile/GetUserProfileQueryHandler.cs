using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Dtos;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.Profile;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserDetailDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDetailDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Getting profile for user: {UserId}", request.UserId);

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Profile requested for non-existent user: {UserId}", request.UserId);
            throw new FshException("User not found");
        }

        var roles = await _userRepository.GetUserRolesAsync(request.UserId)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Profile retrieved for user: {UserId}, {Email}, {Username}",
            request.UserId,
            user.Email.Value,
            user.Username);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber.Value,
            Profession = user.Profession,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            IsIdentityVerified = user.IsIdentityVerified,
            IsActive = user.Status == "ACTIVE",
            Roles = roles
        };
    }
} 