using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserProfile;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserProfile;

public sealed class GetCurrentUserProfileQueryHandler : IQueryHandler<GetCurrentUserProfileQuery, UserDto>
{
    private readonly IUserService _userService;

    public GetCurrentUserProfileQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<UserDto> Handle(GetCurrentUserProfileQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _userService.GetAsync(query.UserId, cancellationToken).ConfigureAwait(false);
    }
}