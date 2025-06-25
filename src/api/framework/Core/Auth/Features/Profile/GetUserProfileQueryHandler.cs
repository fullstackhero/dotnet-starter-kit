using MediatR;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Dtos;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Auth.Features.Profile;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserDetailDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<UserDetailDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(request.UserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            throw new FshException("User not found");
        }

        var roles = await _userRepository.GetUserRolesAsync(request.UserId)
            .ConfigureAwait(false);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber.Value,
            Profession = user.ProfessionId?.ToString() ?? string.Empty,
            Tckn = user.Tckn.Value,
            MemberNumber = user.MemberNumber,
            BirthDate = user.BirthDate,
            CreatedAt = user.CreatedAt,
            IsEmailVerified = user.IsEmailVerified,
            // IsPhoneVerified removed - SMS OTP verification happens during registration
            // IsIdentityVerified removed - MERNIS verification happens during registration
            IsActive = user.Status == "ACTIVE",
            Roles = roles
        };
    }
} 